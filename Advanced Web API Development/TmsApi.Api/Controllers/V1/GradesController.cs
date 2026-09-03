using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.GradeDtos;
using TmsApi.Application.Grading;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class GradesController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly GradingService _gradingService;
    private readonly INotificationService _notificationService;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<TmsApi.Infrastructure.Hubs.TmsHub, TmsApi.Infrastructure.Hubs.ITmsClient> _hubContext;

    public GradesController(
        TmsDbContext context,
        GradingService gradingService,
        INotificationService notificationService,
        Microsoft.AspNetCore.SignalR.IHubContext<TmsApi.Infrastructure.Hubs.TmsHub, TmsApi.Infrastructure.Hubs.ITmsClient> hubContext)
    {
        _context = context;
        _gradingService = gradingService;
        _notificationService = notificationService;
        _hubContext = hubContext;
    }

    public record AssignGradeInput(int EnrollmentId, string? LetterGrade, decimal? NumericScore, decimal? Grade);
    public record BulkGradeInputItem(int EnrollmentId, string? LetterGrade, decimal? NumericScore, decimal? Grade);
    public record BulkAssignInput(IEnumerable<BulkGradeInputItem> Grades);

    [Authorize]
    [HttpGet("student/{studentId:int}")]
    public async Task<IActionResult> GetStudentAcademicRecord(int studentId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var claim = User.FindFirst("studentId")?.Value;
            if (int.TryParse(claim, out var parsed) && parsed != studentId)
            {
                return Forbid();
            }
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student == null) return NotFound(new { message = $"Student with ID {studentId} not found." });

        var enrollments = await _context.Enrollments
            .Include(e => e.Course)
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .ToListAsync(cancellationToken);

        var completed = enrollments.Where(e => e.Status == EnrollmentStatus.Completed || e.Grade.HasValue).ToList();
        var totalCredits = completed.Where(e => e.Course != null).Sum(e => e.Course!.Credits);

        var assessments = await _context.Assessments
            .AsNoTracking()
            .Include(a => a.Course)
            .Where(a => a.StudentId == studentId && a.IsPublished)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            studentId = student.Id,
            studentName = $"{student.FirstName} {student.LastName}",
            gpa = student.Gpa,
            totalCredits,
            completedCoursesCount = completed.Count,
            grades = enrollments.Select(e =>
            {
                var courseAssessments = assessments.Where(a => a.CourseId == e.CourseId).Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    maxScore = a.MaxScore,
                    scoredPoints = a.ScoredPoints,
                    dueDate = a.DueDate,
                    status = a.Status,
                    feedback = a.Feedback,
                    gradedAt = a.GradedAt,
                    rubricScoresJson = a.RubricScoresJson
                }).ToList();

                return new
                {
                    enrollmentId = e.Id,
                    courseId = e.CourseId,
                    courseCode = e.Course?.Code ?? string.Empty,
                    courseTitle = e.Course?.Title ?? string.Empty,
                    credits = e.Course?.Credits ?? 3,
                    numericScore = e.Grade.HasValue ? (e.Grade.Value <= 4.0m ? e.Grade.Value * 25 : e.Grade.Value) : (decimal?)null,
                    letterGrade = e.Grade.HasValue ? _gradingService.CalculateGradeLevel(e.Grade.Value).ToString() : null,
                    status = e.Status.ToString(),
                    assessments = courseAssessments
                };
            })
        });
    }

    [Authorize]
    [HttpGet("my-grades")]
    public async Task<IActionResult> GetMyGrades(CancellationToken cancellationToken)
    {
        int studentId = 1;
        var claim = User.FindFirst("studentId")?.Value;
        if (int.TryParse(claim, out var parsed))
        {
            studentId = parsed;
        }

        return await GetStudentAcademicRecord(studentId, cancellationToken);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("assign")]
    [HttpPost("")]
    public async Task<IActionResult> AssignGrade([FromBody] AssignGradeInput dto, CancellationToken cancellationToken)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId, cancellationToken);

        if (enrollment == null)
        {
            return NotFound(new { message = $"Enrollment with ID {dto.EnrollmentId} not found." });
        }

        decimal numericScore = dto.NumericScore ?? dto.Grade ?? 85m;
        // If score is 0-100 scale, normalize to GPA 0.0 - 4.0 scale
        decimal gpaValue = numericScore > 4.0m ? (numericScore / 100m) * 4.0m : numericScore;

        enrollment.Grade = gpaValue;
        if (gpaValue >= 2.0m)
        {
            enrollment.Status = EnrollmentStatus.Completed;
        }

        var studentGrades = await _context.Enrollments
            .Where(e => e.StudentId == enrollment.StudentId && e.Grade.HasValue && e.Id != enrollment.Id)
            .Select(e => e.Grade!.Value)
            .ToListAsync(cancellationToken);

        studentGrades.Add(gpaValue);
        enrollment.Student.Gpa = _gradingService.CalculateGPA(studentGrades);

        await _context.SaveChangesAsync(cancellationToken);

        var letterGrade = _gradingService.CalculateGradeLevel(gpaValue).ToString();

        // Send notification to student in DB
        var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentId == enrollment.StudentId, cancellationToken);
        if (user != null)
        {
            await _notificationService.CreateNotificationAsync(
                user.Id,
                "Grade Published",
                $"Your final grade for {enrollment.Course?.Code ?? "Course"} ({enrollment.Course?.Title}) is {letterGrade} ({numericScore:F0}%).",
                "Grade",
                enrollment.Id,
                "Enrollment",
                cancellationToken);

            // SignalR live update
            await _hubContext.Clients.Group($"Student_{enrollment.StudentId}").ReceiveNotification(
                "Grade Published",
                $"Your final grade for {enrollment.Course?.Code ?? "Course"} ({enrollment.Course?.Title}) is {letterGrade} ({numericScore:F0}%).");
            await _hubContext.Clients.Group($"Student_{enrollment.StudentId}").ReceiveEnrollmentNotification(new
            {
                studentId = enrollment.StudentId,
                courseId = enrollment.CourseId,
                grade = gpaValue,
                letterGrade
            });
        }

        return Ok(new
        {
            enrollmentId = enrollment.Id,
            grade = gpaValue,
            numericScore,
            updatedGpa = enrollment.Student.Gpa,
            letterGrade
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("bulk-assign")]
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkAssignGrades([FromBody] BulkAssignInput dto, CancellationToken cancellationToken)
    {
        var results = new List<object>();
        var affectedStudentIds = new HashSet<int>();

        foreach (var item in dto.Grades)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == item.EnrollmentId, cancellationToken);

            if (enrollment != null)
            {
                decimal numericScore = item.NumericScore ?? item.Grade ?? 85m;
                decimal gpaValue = numericScore > 4.0m ? (numericScore / 100m) * 4.0m : numericScore;
                enrollment.Grade = gpaValue;
                if (gpaValue >= 2.0m) enrollment.Status = EnrollmentStatus.Completed;

                affectedStudentIds.Add(enrollment.StudentId);

                var letterGrade = _gradingService.CalculateGradeLevel(gpaValue).ToString();
                results.Add(new { enrollmentId = item.EnrollmentId, grade = gpaValue, letterGrade, status = "Updated" });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentId == enrollment.StudentId, cancellationToken);
                if (user != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        "Grade Published",
                        $"Your grade for {enrollment.Course?.Code ?? "Course"} is {letterGrade}.",
                        "Grade",
                        enrollment.Id,
                        "Enrollment",
                        cancellationToken);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Recalculate GPA for all affected students
        foreach (var sId in affectedStudentIds)
        {
            var sGrades = await _context.Enrollments
                .Where(e => e.StudentId == sId && e.Grade.HasValue)
                .Select(e => e.Grade!.Value)
                .ToListAsync(cancellationToken);

            var student = await _context.Students.FindAsync(new object[] { sId }, cancellationToken);
            if (student != null)
            {
                student.Gpa = _gradingService.CalculateGPA(sGrades);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(results);
    }
}
