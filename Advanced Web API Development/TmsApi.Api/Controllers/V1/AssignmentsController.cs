using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AssignmentsController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly IHubContext<TmsHub, ITmsClient> _hubContext;
    private readonly IWebHostEnvironment _env;

    public AssignmentsController(
        TmsDbContext context,
        IHubContext<TmsHub, ITmsClient> hubContext,
        IWebHostEnvironment env)
    {
        _context = context;
        _hubContext = hubContext;
        _env = env;
    }

    public record CreateAssignmentRequest(
        int CourseId,
        int AssignmentNumber,
        int? CourseModuleId,
        int? CourseLessonId,
        string Title,
        string Instructions,
        decimal MaxPoints,
        DateTime DueDate,
        bool IsPublished);

    public record UpdateAssignmentRequest(
        string Title,
        string Instructions,
        decimal MaxPoints,
        DateTime DueDate,
        bool IsPublished);

    public record GradeSubmissionRequest(
        decimal ScoredPoints,
        string? Feedback,
        string? RubricScoresJson);

    private async Task<bool> CanManageCourseAsync(int courseId)
    {
        if (User.IsInRole("Admin")) return true;
        var instructorId = await User.ResolveInstructorIdAsync(_context);
        if (!instructorId.HasValue) return false;

        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        return course != null && course.InstructorId == instructorId.Value;
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("course/{courseId:int}/config")]
    public async Task<IActionResult> GetCourseAssessmentConfig(int courseId, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(courseId)) return Forbid();

        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, cancellationToken);
        if (course == null) return NotFound(new { message = "Course not found." });

        var createdAssignmentNumbers = await _context.Assignments
            .Where(a => a.CourseId == courseId && !a.IsDeleted)
            .OrderBy(a => a.AssignmentNumber)
            .Select(a => a.AssignmentNumber)
            .ToListAsync(cancellationToken);

        var availableNumbers = new List<int>();
        for (int i = 1; i <= course.ExpectedAssignmentCount; i++)
        {
            if (!createdAssignmentNumbers.Contains(i))
            {
                availableNumbers.Add(i);
            }
        }

        return Ok(new
        {
            courseId = course.Id,
            courseTitle = course.Title,
            expectedAssignments = course.ExpectedAssignmentCount,
            expectedMidterm = course.ExpectedMidtermCount,
            expectedFinal = course.ExpectedFinalCount,
            totalAssessments = course.ExpectedAssignmentCount + course.ExpectedMidtermCount + course.ExpectedFinalCount,
            createdAssignmentNumbers = createdAssignmentNumbers,
            availableAssignmentNumbers = availableNumbers,
            createdCount = createdAssignmentNumbers.Count,
            isAllAssignmentsCreated = availableNumbers.Count == 0
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("course/{courseId:int}")]
    public async Task<IActionResult> GetAssignmentsByCourse(int courseId, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(courseId)) return Forbid();

        var assignments = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.CourseModule)
            .Include(a => a.CourseLesson)
            .Include(a => a.Submissions)
            .Where(a => a.CourseId == courseId && !a.IsDeleted)
            .OrderBy(a => a.AssignmentNumber)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        var list = assignments.Select(a => new
        {
            id = a.Id,
            assignmentNumber = a.AssignmentNumber,
            courseId = a.CourseId,
            courseModuleId = a.CourseModuleId,
            moduleTitle = a.CourseModule?.Title,
            courseLessonId = a.CourseLessonId,
            lessonTitle = a.CourseLesson?.Title,
            title = a.Title,
            instructions = a.Instructions,
            maxPoints = a.MaxPoints,
            dueDate = a.DueDate,
            attachmentUrl = a.AttachmentUrl,
            attachmentFileName = a.AttachmentFileName,
            isPublished = a.IsPublished,
            createdAt = a.CreatedAt,
            totalSubmissions = a.Submissions.Count(s => s.Status == "Submitted" || s.Status == "Graded"),
            pendingGrading = a.Submissions.Count(s => s.Status == "Submitted"),
            gradedCount = a.Submissions.Count(s => s.Status == "Graded")
        }).ToList();

        return Ok(list);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAssignmentById(int id, CancellationToken cancellationToken)
    {
        var a = await _context.Assignments
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.CourseModule)
            .Include(x => x.CourseLesson)
            .Include(x => x.Submissions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (a == null) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(a.CourseId)) return Forbid();

        return Ok(new
        {
            id = a.Id,
            assignmentNumber = a.AssignmentNumber,
            courseId = a.CourseId,
            courseTitle = a.Course?.Title,
            courseModuleId = a.CourseModuleId,
            moduleTitle = a.CourseModule?.Title,
            courseLessonId = a.CourseLessonId,
            lessonTitle = a.CourseLesson?.Title,
            title = a.Title,
            instructions = a.Instructions,
            maxPoints = a.MaxPoints,
            dueDate = a.DueDate,
            attachmentUrl = a.AttachmentUrl,
            attachmentFileName = a.AttachmentFileName,
            isPublished = a.IsPublished,
            createdAt = a.CreatedAt,
            totalSubmissions = a.Submissions.Count(s => s.Status == "Submitted" || s.Status == "Graded"),
            pendingGrading = a.Submissions.Count(s => s.Status == "Submitted"),
            gradedCount = a.Submissions.Count(s => s.Status == "Graded")
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(request.CourseId)) return Forbid();

        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CourseId && !c.IsDeleted, cancellationToken);
        if (course == null) return NotFound(new { message = "Course not found." });

        if (request.AssignmentNumber <= 0)
        {
            return BadRequest(new { message = "Assignment number must be greater than 0." });
        }

        if (request.AssignmentNumber > course.ExpectedAssignmentCount)
        {
            return BadRequest(new { message = $"Assignment number {request.AssignmentNumber} exceeds the course's configured assignment count of {course.ExpectedAssignmentCount}." });
        }

        var exists = await _context.Assignments
            .AnyAsync(a => a.CourseId == request.CourseId && a.AssignmentNumber == request.AssignmentNumber && !a.IsDeleted, cancellationToken);

        if (exists)
        {
            return BadRequest(new { message = $"Assignment {request.AssignmentNumber} has already been created for this course." });
        }

        var instructorId = await User.ResolveInstructorIdAsync(_context);

        var assignment = new Assignment
        {
            CourseId = request.CourseId,
            AssignmentNumber = request.AssignmentNumber,
            CourseModuleId = request.CourseModuleId,
            CourseLessonId = request.CourseLessonId,
            Title = request.Title,
            Instructions = request.Instructions,
            MaxPoints = request.MaxPoints > 0 ? request.MaxPoints : 100m,
            DueDate = request.DueDate,
            IsPublished = request.IsPublished,
            CreatedByInstructorId = instructorId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Assignments.AddAsync(assignment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Populate pending assessment rows for currently enrolled active students
        var enrollments = await _context.Enrollments
            .Where(e => e.CourseId == request.CourseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var en in enrollments)
        {
            _context.Assessments.Add(new Assessment
            {
                StudentId = en.StudentId,
                CourseId = request.CourseId,
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                MaxScore = assignment.MaxPoints,
                DueDate = assignment.DueDate,
                Status = "Pending",
                IsPublished = assignment.IsPublished
            });
        }
        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.ReceiveAssignmentUpdate(new { action = "created", assignmentId = assignment.Id, courseId = assignment.CourseId });

        return Ok(new { id = assignment.Id, message = "Assignment created successfully." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _context.Assignments.FindAsync(new object[] { id }, cancellationToken);
        if (assignment == null || assignment.IsDeleted) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        assignment.Title = request.Title;
        assignment.Instructions = request.Instructions;
        assignment.MaxPoints = request.MaxPoints > 0 ? request.MaxPoints : assignment.MaxPoints;
        assignment.DueDate = request.DueDate;
        assignment.IsPublished = request.IsPublished;
        assignment.UpdatedAt = DateTime.UtcNow;

        // Update corresponding assessment records title & max points
        var assessments = await _context.Assessments.Where(a => a.AssignmentId == id).ToListAsync(cancellationToken);
        foreach (var ass in assessments)
        {
            ass.Title = request.Title;
            ass.MaxScore = assignment.MaxPoints;
            ass.DueDate = assignment.DueDate;
            ass.IsPublished = assignment.IsPublished;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Assignment updated successfully." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAssignment(int id, CancellationToken cancellationToken)
    {
        var assignment = await _context.Assignments.FindAsync(new object[] { id }, cancellationToken);
        if (assignment == null || assignment.IsDeleted) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        assignment.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/attachment")]
    public async Task<IActionResult> UploadAssignmentAttachment(int id, IFormFile file)
    {
        var assignment = await _context.Assignments.FindAsync(id);
        if (assignment == null || assignment.IsDeleted) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
        if (file.Length > 20 * 1024 * 1024) return BadRequest(new { message = "File exceeds 20MB limit." });

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "assignment_attachments");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var uniqueName = $"assign_{id}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        assignment.AttachmentUrl = $"/uploads/assignment_attachments/{uniqueName}";
        assignment.AttachmentFileName = file.FileName;
        await _context.SaveChangesAsync();

        return Ok(new { attachmentUrl = assignment.AttachmentUrl, attachmentFileName = assignment.AttachmentFileName });
    }

    // ─── Submissions & Inline Review ─────────────────────────────────────────

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{id:int}/submissions")]
    public async Task<IActionResult> GetAssignmentSubmissions(int id, CancellationToken cancellationToken)
    {
        var assignment = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);

        if (assignment == null) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        var submissions = await _context.Assessments
            .AsNoTracking()
            .Include(s => s.Student)
                .ThenInclude(st => st.User)
            .Where(s => s.AssignmentId == id &&
                        (s.Status == "Submitted" || s.Status == "Graded") &&
                        s.SubmittedAt != null &&
                        (!string.IsNullOrEmpty(s.SubmissionText) || !string.IsNullOrEmpty(s.SubmissionFileUrl)))
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(cancellationToken);

        var list = submissions.Select(s => new
        {
            id = s.Id,
            assignmentId = s.AssignmentId,
            assignmentTitle = assignment.Title,
            maxPoints = assignment.MaxPoints,
            studentId = s.StudentId,
            studentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : "Student",
            studentEmail = s.Student?.Email,
            studentRegistrationNumber = s.Student?.RegistrationNumber,
            studentAvatar = s.Student?.User?.ProfilePictureUrl ?? s.Student?.ProfilePictureUrl,
            submittedAt = s.SubmittedAt,
            status = s.Status,
            scoredPoints = s.ScoredPoints,
            feedback = s.Feedback,
            submissionFileUrl = s.SubmissionFileUrl,
            submissionText = s.SubmissionText,
            gradedAt = s.GradedAt
        }).ToList();

        return Ok(list);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("submissions/{submissionId:int}")]
    public async Task<IActionResult> GetSubmissionDetail(int submissionId, CancellationToken cancellationToken)
    {
        var s = await _context.Assessments
            .AsNoTracking()
            .Include(x => x.Assignment)
            .Include(x => x.Course)
            .Include(x => x.Student)
                .ThenInclude(st => st.User)
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        if (s == null) return NotFound(new { message = "Submission not found." });
        if (!await CanManageCourseAsync(s.CourseId)) return Forbid();

        return Ok(new
        {
            id = s.Id,
            assignmentId = s.AssignmentId,
            assignmentTitle = s.Assignment?.Title ?? s.Title,
            assignmentInstructions = s.Assignment?.Instructions,
            assignmentAttachmentUrl = s.Assignment?.AttachmentUrl,
            maxPoints = s.MaxScore,
            courseId = s.CourseId,
            courseTitle = s.Course?.Title,
            studentId = s.StudentId,
            studentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : "Student",
            studentEmail = s.Student?.Email,
            studentRegistrationNumber = s.Student?.RegistrationNumber,
            studentAvatar = s.Student?.User?.ProfilePictureUrl ?? s.Student?.ProfilePictureUrl,
            submittedAt = s.SubmittedAt,
            dueDate = s.DueDate,
            status = s.Status,
            scoredPoints = s.ScoredPoints,
            feedback = s.Feedback,
            rubricScoresJson = s.RubricScoresJson,
            submissionFileUrl = s.SubmissionFileUrl,
            submissionText = s.SubmissionText,
            gradedAt = s.GradedAt
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("submissions/{submissionId:int}/grade")]
    public async Task<IActionResult> GradeSubmission(int submissionId, [FromBody] GradeSubmissionRequest request, CancellationToken cancellationToken)
    {
        var assessment = await _context.Assessments
            .Include(a => a.Student)
            .Include(a => a.Course)
            .Include(a => a.Assignment)
            .FirstOrDefaultAsync(a => a.Id == submissionId, cancellationToken);

        if (assessment == null) return NotFound(new { message = "Submission not found." });
        if (!await CanManageCourseAsync(assessment.CourseId)) return Forbid();

        assessment.ScoredPoints = request.ScoredPoints;
        assessment.Feedback = request.Feedback;
        assessment.RubricScoresJson = request.RubricScoresJson;
        assessment.Status = "Graded";
        assessment.GradedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Notify student via SignalR
        if (assessment.Student != null)
        {
            var title = $"Graded: {assessment.Title}";
            var message = $"Your assignment '{assessment.Title}' in {assessment.Course?.Title} was graded: {request.ScoredPoints}/{assessment.MaxScore} pts.";
            await _hubContext.Clients.Group($"Student_{assessment.StudentId}").ReceiveNotification(title, message);
            await _hubContext.Clients.Group($"Student_{assessment.StudentId}").ReceiveGradePublished(new
            {
                assessmentId = assessment.Id,
                courseId = assessment.CourseId,
                scoredPoints = assessment.ScoredPoints,
                maxScore = assessment.MaxScore
            });
        }

        return Ok(new
        {
            message = "Submission graded successfully.",
            status = assessment.Status,
            scoredPoints = assessment.ScoredPoints,
            gradedAt = assessment.GradedAt
        });
    }

    // ─── Student Assignment Viewing & Submission ─────────────────────────────

    [Authorize(Roles = "Student,Admin")]
    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue) return Forbid("User is not linked to a student account.");

        var activeEnrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId.Value &&
                        e.Status != EnrollmentStatus.Dropped &&
                        e.Status != EnrollmentStatus.Rejected &&
                        !e.IsDeleted && !e.IsArchived &&
                        e.Course != null && !e.Course.IsDeleted)
            .ToListAsync(cancellationToken);

        var enrolledCourseIds = activeEnrollments.Select(e => e.CourseId).ToList();

        var assignments = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.CourseModule)
            .Include(a => a.CourseLesson)
            .Where(a => enrolledCourseIds.Contains(a.CourseId) && !a.IsDeleted && a.IsPublished)
            .OrderBy(a => a.DueDate)
            .ToListAsync(cancellationToken);

        var studentSubmissions = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.StudentId == studentId.Value && enrolledCourseIds.Contains(a.CourseId))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var list = assignments.Select(a =>
        {
            var sub = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id);
            var status = sub?.Status ?? "Pending";
            var isPastDue = now > a.DueDate;
            var canSubmit = status != "Graded" && !isPastDue;

            return new
            {
                id = a.Id,
                submissionId = sub?.Id,
                courseId = a.CourseId,
                courseTitle = a.Course?.Title ?? "Course",
                courseCode = a.Course?.Code ?? "",
                courseModuleId = a.CourseModuleId,
                moduleTitle = a.CourseModule?.Title,
                courseLessonId = a.CourseLessonId,
                lessonTitle = a.CourseLesson?.Title,
                title = a.Title,
                instructions = a.Instructions,
                maxPoints = a.MaxPoints,
                dueDate = a.DueDate,
                attachmentUrl = a.AttachmentUrl,
                attachmentFileName = a.AttachmentFileName,
                status = status,
                submittedAt = sub?.SubmittedAt,
                scoredPoints = sub?.ScoredPoints,
                feedback = sub?.Feedback,
                gradedAt = sub?.GradedAt,
                submissionFileUrl = sub?.SubmissionFileUrl,
                submissionFileName = sub?.SubmissionFileName,
                submissionFileType = sub?.SubmissionFileType,
                submissionFileSize = sub?.SubmissionFileSize,
                submissionText = sub?.SubmissionText,
                isPastDue = isPastDue,
                canSubmit = canSubmit
            };
        }).ToList();

        return Ok(list);
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpGet("{id:int}/student-view")]
    public async Task<IActionResult> GetStudentAssignmentDetail(int id, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue) return StatusCode(StatusCodes.Status403Forbidden, new { message = "User is not linked to a student account." });

        var assignment = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.CourseModule)
            .Include(a => a.CourseLesson)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.IsPublished, cancellationToken);

        if (assignment == null) return NotFound(new { message = "Assignment not found." });

        var isEnrolled = await _context.Enrollments.AnyAsync(e =>
            e.StudentId == studentId.Value &&
            e.CourseId == assignment.CourseId &&
            e.Status != EnrollmentStatus.Dropped &&
            e.Status != EnrollmentStatus.Rejected &&
            !e.IsDeleted && !e.IsArchived, cancellationToken);

        if (!isEnrolled) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not actively enrolled in this course." });

        var sub = await _context.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.AssignmentId == id && s.StudentId == studentId.Value, cancellationToken);

        var now = DateTime.UtcNow;
        var status = sub?.Status ?? "Pending";
        var isPastDue = now > assignment.DueDate;
        var canSubmit = status != "Graded" && !isPastDue;

        return Ok(new
        {
            id = assignment.Id,
            submissionId = sub?.Id,
            courseId = assignment.CourseId,
            courseTitle = assignment.Course?.Title ?? "Course",
            courseCode = assignment.Course?.Code ?? "",
            courseModuleId = assignment.CourseModuleId,
            moduleTitle = assignment.CourseModule?.Title,
            courseLessonId = assignment.CourseLessonId,
            lessonTitle = assignment.CourseLesson?.Title,
            title = assignment.Title,
            instructions = assignment.Instructions,
            maxPoints = assignment.MaxPoints,
            dueDate = assignment.DueDate,
            attachmentUrl = assignment.AttachmentUrl,
            attachmentFileName = assignment.AttachmentFileName,
            status = status,
            submittedAt = sub?.SubmittedAt,
            scoredPoints = sub?.ScoredPoints,
            feedback = sub?.Feedback,
            gradedAt = sub?.GradedAt,
            submissionFileUrl = sub?.SubmissionFileUrl,
            submissionFileName = sub?.SubmissionFileName,
            submissionFileType = sub?.SubmissionFileType,
            submissionFileSize = sub?.SubmissionFileSize,
            submissionText = sub?.SubmissionText,
            isPastDue = isPastDue,
            canSubmit = canSubmit
        });
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> SubmitAssignment(int id, [FromForm] IFormFile? file, [FromForm] string? submissionText, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue) return StatusCode(StatusCodes.Status403Forbidden, new { message = "User is not linked to a student account." });

        var student = await _context.Students.FindAsync(new object[] { studentId.Value }, cancellationToken);
        if (student == null) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student profile not found." });

        var assignment = await _context.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.IsPublished, cancellationToken);

        if (assignment == null) return NotFound(new { message = "Assignment not found." });

        var isEnrolled = await _context.Enrollments.AnyAsync(e =>
            e.StudentId == studentId.Value &&
            e.CourseId == assignment.CourseId &&
            e.Status != EnrollmentStatus.Dropped &&
            e.Status != EnrollmentStatus.Rejected &&
            !e.IsDeleted && !e.IsArchived, cancellationToken);

        if (!isEnrolled) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not actively enrolled in this course." });

        // Rule: Deadlines enforced on backend
        if (DateTime.UtcNow > assignment.DueDate)
        {
            return BadRequest(new { message = "Assignment submission deadline has passed. Late submissions are not accepted." });
        }

        // Rule: Must provide at least file or written response with meaningful content
        if ((file == null || file.Length == 0) && string.IsNullOrWhiteSpace(submissionText))
        {
            return BadRequest(new
            {
                message = "Assignment submission validation failed.",
                errors = new
                {
                    submission = new[] { "Add a response or attachment before submitting." }
                }
            });
        }

        string? fileUrl = null;
        string? fileName = null;
        string? fileType = null;
        long? fileSize = null;

        if (file != null && file.Length > 0)
        {
            // Rule: Max file size 25MB
            if (file.Length > 25 * 1024 * 1024)
            {
                return BadRequest(new { message = "Uploaded file exceeds the maximum 25MB limit." });
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".zip", ".txt", ".png", ".jpg", ".jpeg", ".csv", ".xlsx" };
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest(new { message = $"File extension '{ext}' is not supported. Allowed formats: PDF, Word, ZIP, Text, Images." });
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "submissions");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueName = $"sub_{studentId.Value}_{assignment.Id}_{DateTime.UtcNow.Ticks}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            fileUrl = $"/uploads/submissions/{uniqueName}";
            fileName = file.FileName;
            fileType = ext;
            fileSize = file.Length;
        }

        // Check if an existing assessment record exists for this assignment & student
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.AssignmentId == id && a.StudentId == studentId.Value, cancellationToken);

        if (assessment != null)
        {
            // Rule: Cannot resubmit once graded
            if (assessment.Status == "Graded")
            {
                return BadRequest(new { message = "This assignment has already been evaluated and graded. Submissions are closed." });
            }

            // Update existing assessment record (resubmission)
            assessment.Title = assignment.Title;
            assessment.MaxScore = assignment.MaxPoints;
            assessment.DueDate = assignment.DueDate;
            assessment.SubmittedAt = DateTime.UtcNow;
            assessment.Status = "Submitted";
            if (!string.IsNullOrWhiteSpace(submissionText)) assessment.SubmissionText = submissionText;
            if (fileUrl != null)
            {
                assessment.SubmissionFileUrl = fileUrl;
                assessment.SubmissionFileName = fileName;
                assessment.SubmissionFileType = fileType;
                assessment.SubmissionFileSize = fileSize;
            }
        }
        else
        {
            // Create new Assessment record
            assessment = new Assessment
            {
                StudentId = studentId.Value,
                CourseId = assignment.CourseId,
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                MaxScore = assignment.MaxPoints,
                DueDate = assignment.DueDate,
                SubmittedAt = DateTime.UtcNow,
                Status = "Submitted",
                IsPublished = true,
                SubmissionText = submissionText,
                SubmissionFileUrl = fileUrl,
                SubmissionFileName = fileName,
                SubmissionFileType = fileType,
                SubmissionFileSize = fileSize
            };
            await _context.Assessments.AddAsync(assessment, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Real-time SignalR notifications
        await _hubContext.Clients.All.ReceiveAssignmentUpdate(new
        {
            action = "submitted",
            assignmentId = assignment.Id,
            courseId = assignment.CourseId,
            studentId = studentId.Value
        });

        await _hubContext.Clients.All.ReceiveEnrollmentNotification(new
        {
            action = "submission",
            studentId = studentId.Value,
            courseId = assignment.CourseId
        });

        // Notify instructor if present
        if (assignment.CreatedByInstructorId.HasValue)
        {
            await _hubContext.Clients.Group($"Instructor_{assignment.CreatedByInstructorId.Value}")
                .ReceiveNotification("New Student Submission", $"{student.FirstName} {student.LastName} submitted '{assignment.Title}'.");
        }

        return Ok(new
        {
            message = "Assignment submitted successfully.",
            submissionId = assessment.Id,
            assignmentId = assignment.Id,
            status = assessment.Status,
            submittedAt = assessment.SubmittedAt,
            fileName = assessment.SubmissionFileName,
            fileUrl = assessment.SubmissionFileUrl,
            submissionText = assessment.SubmissionText
        });
    }
}
