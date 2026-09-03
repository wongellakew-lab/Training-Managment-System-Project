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
            var currentStudentId = await TmsApi.Api.Extensions.ClaimsExtensions.ResolveStudentIdAsync(User, _context);
            if (!currentStudentId.HasValue || currentStudentId.Value != studentId)
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
            .Where(e => e.StudentId == studentId && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected && !e.IsDeleted)
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
        var currentStudentId = await TmsApi.Api.Extensions.ClaimsExtensions.ResolveStudentIdAsync(User, _context);
        if (!currentStudentId.HasValue)
        {
            return NotFound(new { message = "Student record not found for authenticated user." });
        }

        return await GetStudentAcademicRecord(currentStudentId.Value, cancellationToken);
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
        var notifUserId = user?.Id ?? enrollment.Student.UserId ?? enrollment.StudentId.ToString();

        await _notificationService.CreateNotificationAsync(
            notifUserId,
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
        await _hubContext.Clients.Group($"Student_{enrollment.StudentId}").ReceiveGradePublished(new
        {
            studentId = enrollment.StudentId,
            courseId = enrollment.CourseId,
            grade = gpaValue,
            letterGrade
        });

        if (user != null)
        {
            await _hubContext.Clients.Group($"User_{user.Id}").ReceiveNotification(
                "Grade Published",
                $"Your final grade for {enrollment.Course?.Code ?? "Course"} ({enrollment.Course?.Title}) is {letterGrade} ({numericScore:F0}%).");
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

    // ─── Instructor Gradebook Spreadsheet Matrix ─────────────────────────────

    public record GradebookCellRequest(int CourseId, int StudentId, int AssignmentId, decimal? Score, string? Feedback);

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("gradebook/course/{courseId:int}")]
    public async Task<IActionResult> GetCourseGradebook(int courseId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            var instructorId = await TmsApi.Api.Extensions.ClaimsExtensions.ResolveInstructorIdAsync(User, _context);
            var courseOwner = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
            if (courseOwner == null || courseOwner.InstructorId != instructorId) return Forbid();
        }

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, cancellationToken);

        if (course == null) return NotFound(new { message = "Course not found." });

        var assignments = await _context.Assignments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && !a.IsDeleted && a.IsPublished)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new
            {
                id = a.Id,
                title = a.Title,
                maxPoints = a.MaxPoints,
                dueDate = a.DueDate
            })
            .ToListAsync(cancellationToken);

        var assignmentIds = assignments.Select(a => a.id).ToList();

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
                .ThenInclude(s => s.User)
            .Where(e => e.CourseId == courseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .OrderBy(e => e.Student.LastName)
            .ThenBy(e => e.Student.FirstName)
            .ToListAsync(cancellationToken);

        var assessments = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && a.AssignmentId.HasValue && assignmentIds.Contains(a.AssignmentId.Value))
            .ToListAsync(cancellationToken);

        var rows = enrollments.Select(e =>
        {
            var st = e.Student;
            var studentAssessments = assessments.Where(a => a.StudentId == st.Id).ToList();

            var gradeCells = assignments.Select(asgn =>
            {
                var match = studentAssessments.FirstOrDefault(a => a.AssignmentId == asgn.id);
                return new
                {
                    assignmentId = asgn.id,
                    assessmentId = match?.Id,
                    score = match?.ScoredPoints,
                    maxScore = match?.MaxScore ?? asgn.maxPoints,
                    status = match?.Status ?? "Pending",
                    isGraded = match != null && match.Status == "Graded" && match.ScoredPoints.HasValue,
                    feedback = match?.Feedback
                };
            }).ToList();

            var gradedCells = gradeCells.Where(g => g.isGraded && g.score.HasValue).ToList();
            var totalScore = gradedCells.Sum(g => g.score!.Value);
            var totalMaxScore = gradedCells.Sum(g => g.maxScore);
            var avgPercentage = totalMaxScore > 0 ? Math.Round((totalScore / totalMaxScore) * 100m, 1) : 0m;
            var letter = _gradingService.CalculateGradeLevel(avgPercentage).ToString();

            return new
            {
                studentId = st.Id,
                studentName = $"{st.FirstName} {st.LastName}",
                registrationNumber = st.RegistrationNumber,
                avatarUrl = st.User?.ProfilePictureUrl ?? st.ProfilePictureUrl,
                grades = gradeCells,
                totalScore,
                totalMaxScore,
                averagePercentage = avgPercentage,
                letterGrade = letter
            };
        }).ToList();

        return Ok(new
        {
            courseId = course.Id,
            courseTitle = course.Title,
            courseCode = course.Code,
            assignments,
            rows
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("gradebook/cell")]
    public async Task<IActionResult> UpdateGradebookCell([FromBody] GradebookCellRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            var instructorId = await TmsApi.Api.Extensions.ClaimsExtensions.ResolveInstructorIdAsync(User, _context);
            var courseOwner = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);
            if (courseOwner == null || courseOwner.InstructorId != instructorId) return Forbid();
        }

        var assignment = await _context.Assignments.FindAsync(new object[] { request.AssignmentId }, cancellationToken);
        if (assignment == null) return NotFound(new { message = "Assignment not found." });

        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.CourseId == request.CourseId &&
                                      a.StudentId == request.StudentId &&
                                      a.AssignmentId == request.AssignmentId, cancellationToken);

        if (assessment == null)
        {
            assessment = new Assessment
            {
                CourseId = request.CourseId,
                StudentId = request.StudentId,
                AssignmentId = request.AssignmentId,
                Title = assignment.Title,
                MaxScore = assignment.MaxPoints,
                DueDate = assignment.DueDate,
                IsPublished = true
            };
            await _context.Assessments.AddAsync(assessment, cancellationToken);
        }

        if (request.Score.HasValue)
        {
            assessment.ScoredPoints = request.Score.Value;
            assessment.Status = "Graded";
            assessment.GradedAt = DateTime.UtcNow;
            if (request.Feedback != null) assessment.Feedback = request.Feedback;
        }
        else
        {
            assessment.ScoredPoints = null;
            assessment.Status = "Pending";
            assessment.GradedAt = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Grade updated successfully.",
            assessmentId = assessment.Id,
            studentId = assessment.StudentId,
            assignmentId = assessment.AssignmentId,
            score = assessment.ScoredPoints,
            status = assessment.Status
        });
    }
}
