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

        // Compute cumulative GPA across all courses on 4.00 scale
        var gradedEnrollments = enrollments.Where(e => e.Grade.HasValue && e.Course != null).ToList();
        decimal calculatedGpa = 0.0m;
        if (gradedEnrollments.Any())
        {
            decimal totalPoints = 0m;
            int gpaCredits = 0;
            foreach (var ge in gradedEnrollments)
            {
                var cr = ge.Course?.Credits > 0 ? ge.Course.Credits : 3;
                decimal raw = ge.Grade!.Value;
                decimal gp = raw switch
                {
                    > 4.0m and >= 90m => 4.0m,
                    > 4.0m and >= 80m => 3.0m,
                    > 4.0m and >= 70m => 2.0m,
                    > 4.0m and >= 60m => 1.0m,
                    > 4.0m => 0.0m,
                    _ => Math.Min(4.0m, Math.Max(0.0m, raw))
                };
                totalPoints += gp * cr;
                gpaCredits += cr;
            }
            calculatedGpa = gpaCredits > 0 ? Math.Round(totalPoints / gpaCredits, 2) : 0.0m;
        }
        else if (student.Gpa > 0)
        {
            calculatedGpa = student.Gpa > 4.0m
                ? (student.Gpa >= 90m ? 4.0m : student.Gpa >= 80m ? 3.0m : student.Gpa >= 70m ? 2.0m : student.Gpa >= 60m ? 1.0m : 0.0m)
                : Math.Round(student.Gpa, 2);
        }

        var assessments = await _context.Assessments
            .AsNoTracking()
            .Include(a => a.Course)
            .Where(a => a.StudentId == studentId && a.IsPublished)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            studentId = student.Id,
            studentName = $"{student.FirstName} {student.LastName}",
            gpa = calculatedGpa,
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

            var requiredCount = assignments.Count;
            var gradedCells = gradeCells.Where(g => g.isGraded && g.score.HasValue).ToList();
            var gradedCount = gradedCells.Count;
            var isGradingComplete = requiredCount > 0 && gradedCount == requiredCount;

            decimal? totalScore = isGradingComplete ? gradedCells.Sum(g => g.score!.Value) : null;
            decimal? totalMaxScore = isGradingComplete ? assignments.Sum(a => a.maxPoints) : null;
            decimal? avgPercentage = isGradingComplete && totalMaxScore > 0 ? Math.Round((totalScore!.Value / totalMaxScore.Value) * 100m, 1) : null;
            string letter = isGradingComplete && avgPercentage.HasValue ? _gradingService.CalculateGradeLevel(avgPercentage.Value).ToString() : "Pending";

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
                letterGrade = letter,
                isGradingComplete,
                gradedAssessmentCount = gradedCount,
                requiredAssessmentCount = requiredCount
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

    public record BulkGradebookItem(int StudentId, int AssignmentId, decimal? Score, string? Feedback);
    public record BulkGradebookRequest(int CourseId, List<BulkGradebookItem> Grades);

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("gradebook/bulk")]
    public async Task<IActionResult> SubmitBulkGradebook([FromBody] BulkGradebookRequest request, CancellationToken cancellationToken)
    {
        if (request.Grades == null || !request.Grades.Any())
        {
            return BadRequest(new { message = "No grades submitted." });
        }

        if (User?.IsInRole("Instructor") == true && !User.IsInRole("Admin"))
        {
            var instructorId = await TmsApi.Api.Extensions.ClaimsExtensions.ResolveInstructorIdAsync(User, _context);
            var courseOwner = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);
            if (courseOwner == null || courseOwner.InstructorId != instructorId) return Forbid();
        }

        var assignmentIds = request.Grades.Select(g => g.AssignmentId).Distinct().ToList();
        var assignments = await _context.Assignments
            .Where(a => a.CourseId == request.CourseId && assignmentIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        // Validation pass: ensure all grades are provided and within [0, maxPoints]
        foreach (var item in request.Grades)
        {
            if (!assignments.TryGetValue(item.AssignmentId, out var assignment))
            {
                return BadRequest(new { message = $"Assignment with ID {item.AssignmentId} not found in this course." });
            }

            if (!item.Score.HasValue)
            {
                return BadRequest(new
                {
                    message = "Grade validation failed.",
                    errors = new
                    {
                        score = new[] { "Grade is required." }
                    }
                });
            }

            if (item.Score.Value < 0)
            {
                return BadRequest(new { message = $"Grade cannot be negative. Found {item.Score.Value} for assignment '{assignment.Title}'." });
            }
            if (item.Score.Value > assignment.MaxPoints)
            {
                return BadRequest(new { message = $"Grade cannot exceed maximum points ({assignment.MaxPoints}). Found {item.Score.Value} for assignment '{assignment.Title}'." });
            }
        }

        // Fetch existing assessments
        var studentIds = request.Grades.Select(g => g.StudentId).Distinct().ToList();
        var existingAssessments = await _context.Assessments
            .Where(a => a.CourseId == request.CourseId &&
                        studentIds.Contains(a.StudentId) &&
                        assignmentIds.Contains(a.AssignmentId!.Value))
            .ToListAsync(cancellationToken);

        var affectedStudentIds = new HashSet<int>();

        foreach (var item in request.Grades)
        {
            var assignment = assignments[item.AssignmentId];
            var assessment = existingAssessments.FirstOrDefault(a => a.StudentId == item.StudentId && a.AssignmentId == item.AssignmentId);

            if (assessment == null)
            {
                assessment = new Assessment
                {
                    CourseId = request.CourseId,
                    StudentId = item.StudentId,
                    AssignmentId = item.AssignmentId,
                    Title = assignment.Title,
                    MaxScore = assignment.MaxPoints,
                    DueDate = assignment.DueDate,
                    IsPublished = true
                };
                await _context.Assessments.AddAsync(assessment, cancellationToken);
                existingAssessments.Add(assessment);
            }

            if (item.Score.HasValue)
            {
                assessment.ScoredPoints = item.Score.Value;
                assessment.Status = "Graded";
                assessment.GradedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(item.Feedback)) assessment.Feedback = item.Feedback;
                affectedStudentIds.Add(item.StudentId);
            }
            else
            {
                assessment.ScoredPoints = null;
                assessment.Status = "Pending";
                assessment.GradedAt = null;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Recalculate enrollment grades and course completion for affected students
        if (affectedStudentIds.Any())
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.CourseId == request.CourseId && affectedStudentIds.Contains(e.StudentId) && !e.IsDeleted)
                .ToListAsync(cancellationToken);

            var totalAssignmentsCount = await _context.Assignments
                .CountAsync(a => a.CourseId == request.CourseId && !a.IsDeleted && a.IsPublished, cancellationToken);

            var allStudentAssessments = await _context.Assessments
                .Where(a => a.CourseId == request.CourseId && affectedStudentIds.Contains(a.StudentId))
                .ToListAsync(cancellationToken);

            foreach (var en in enrollments)
            {
                var sAssessments = allStudentAssessments.Where(a => a.StudentId == en.StudentId).ToList();
                var gradedAssessments = sAssessments.Where(a => a.ScoredPoints.HasValue).ToList();

                if (gradedAssessments.Any())
                {
                    decimal totalScored = gradedAssessments.Sum(a => a.ScoredPoints!.Value);
                    decimal totalMax = gradedAssessments.Sum(a => a.MaxScore);
                    decimal avgPct = totalMax > 0 ? Math.Round((totalScored / totalMax) * 100m, 1) : 0m;
                    en.Grade = avgPct;

                    if (totalAssignmentsCount > 0 && gradedAssessments.Count >= totalAssignmentsCount && avgPct >= 60m)
                    {
                        en.Status = EnrollmentStatus.Completed;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        // Notify affected students via SignalR
        foreach (var sId in affectedStudentIds)
        {
            await _hubContext.Clients.Group($"Student_{sId}").ReceiveGradePublished(new
            {
                courseId = request.CourseId,
                studentId = sId,
                message = "Grades have been updated."
            });
        }
        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "gradesSubmitted", courseId = request.CourseId });

        return Ok(new
        {
            message = "Grades submitted successfully",
            updatedCount = request.Grades.Count
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

        if (!request.Score.HasValue)
        {
            return BadRequest(new
            {
                message = "Grade validation failed.",
                errors = new
                {
                    score = new[] { "Grade is required." }
                }
            });
        }

        if (request.Score.Value < 0)
        {
            return BadRequest(new { message = "Grade cannot be negative." });
        }
        if (request.Score.Value > assignment.MaxPoints)
        {
            return BadRequest(new { message = $"Grade cannot exceed the maximum score of {assignment.MaxPoints}." });
        }

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

        // Recalculate enrollment grade and completion for this student
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.StudentId == request.StudentId && !e.IsDeleted, cancellationToken);

        if (enrollment != null)
        {
            var totalAssignmentsCount = await _context.Assignments
                .CountAsync(a => a.CourseId == request.CourseId && !a.IsDeleted && a.IsPublished, cancellationToken);

            var studentAssessments = await _context.Assessments
                .Where(a => a.CourseId == request.CourseId && a.StudentId == request.StudentId)
                .ToListAsync(cancellationToken);

            var gradedAssessments = studentAssessments.Where(a => a.ScoredPoints.HasValue).ToList();
            if (gradedAssessments.Any())
            {
                decimal totalScored = gradedAssessments.Sum(a => a.ScoredPoints!.Value);
                decimal totalMax = gradedAssessments.Sum(a => a.MaxScore);
                decimal avgPct = totalMax > 0 ? Math.Round((totalScored / totalMax) * 100m, 1) : 0m;
                enrollment.Grade = avgPct;

                if (totalAssignmentsCount > 0 && gradedAssessments.Count >= totalAssignmentsCount && avgPct >= 60m)
                {
                    enrollment.Status = EnrollmentStatus.Completed;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Notify student via SignalR
        if (request.Score.HasValue)
        {
            await _hubContext.Clients.Group($"Student_{request.StudentId}").ReceiveGradePublished(new
            {
                courseId = request.CourseId,
                studentId = request.StudentId,
                assignmentId = request.AssignmentId,
                scoredPoints = request.Score.Value,
                maxScore = assignment.MaxPoints,
                message = $"Grade published for '{assignment.Title}'."
            });
        }

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
