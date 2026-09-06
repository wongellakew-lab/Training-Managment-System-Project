using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.AssignmentDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class AssignmentService(
    TmsDbContext _context,
    IHubContext<TmsHub, ITmsClient> _hubContext,
    IWebHostEnvironment _env,
    ICertificateService _certificateService) : IAssignmentService
{

    public async Task<object?> GetCourseAssessmentConfigAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, cancellationToken);
        if (course == null) return null;

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

        return new
        {
            courseId = course.Id,
            courseTitle = course.Title,
            expectedAssignments = course.ExpectedAssignmentCount,
            expectedMidterm = course.ExpectedMidtermCount,
            expectedFinal = course.ExpectedFinalCount,
            totalAssessments = course.ExpectedAssignmentCount + course.ExpectedMidtermCount + course.ExpectedFinalCount,
            createdAssignmentNumbers,
            availableAssignmentNumbers = availableNumbers,
            createdCount = createdAssignmentNumbers.Count,
            isAllAssignmentsCreated = availableNumbers.Count == 0
        };
    }

    public async Task<List<AssignmentDto>> GetAssignmentsByCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var assignments = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.CourseModule)
            .Include(a => a.CourseLesson)
            .Include(a => a.Submissions)
            .Where(a => a.CourseId == courseId && !a.IsDeleted)
            .OrderBy(a => a.AssignmentNumber)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return assignments.Select(a => new AssignmentDto
        {
            Id = a.Id,
            AssignmentNumber = a.AssignmentNumber,
            CourseId = a.CourseId,
            CourseModuleId = a.CourseModuleId,
            ModuleTitle = a.CourseModule?.Title,
            CourseLessonId = a.CourseLessonId,
            LessonTitle = a.CourseLesson?.Title,
            Title = a.Title,
            Instructions = a.Instructions,
            MaxPoints = a.MaxPoints,
            DueDate = a.DueDate,
            AttachmentUrl = a.AttachmentUrl,
            AttachmentFileName = a.AttachmentFileName,
            IsPublished = a.IsPublished,
            CreatedAt = a.CreatedAt,
            TotalSubmissions = a.Submissions.Count(s => s.Status == "Submitted" || s.Status == "Graded"),
            PendingGrading = a.Submissions.Count(s => s.Status == "Submitted"),
            GradedCount = a.Submissions.Count(s => s.Status == "Graded")
        }).ToList();
    }

    public async Task<AssignmentDto?> GetAssignmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var a = await _context.Assignments
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.CourseModule)
            .Include(x => x.CourseLesson)
            .Include(x => x.Submissions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (a == null) return null;

        return new AssignmentDto
        {
            Id = a.Id,
            AssignmentNumber = a.AssignmentNumber,
            CourseId = a.CourseId,
            CourseTitle = a.Course?.Title,
            CourseModuleId = a.CourseModuleId,
            ModuleTitle = a.CourseModule?.Title,
            CourseLessonId = a.CourseLessonId,
            LessonTitle = a.CourseLesson?.Title,
            Title = a.Title,
            Instructions = a.Instructions,
            MaxPoints = a.MaxPoints,
            DueDate = a.DueDate,
            AttachmentUrl = a.AttachmentUrl,
            AttachmentFileName = a.AttachmentFileName,
            IsPublished = a.IsPublished,
            CreatedAt = a.CreatedAt,
            TotalSubmissions = a.Submissions.Count(s => s.Status == "Submitted" || s.Status == "Graded"),
            PendingGrading = a.Submissions.Count(s => s.Status == "Submitted"),
            GradedCount = a.Submissions.Count(s => s.Status == "Graded")
        };
    }

    public async Task<int> CreateAssignmentAsync(CreateAssignmentDto dto, int? instructorId, CancellationToken cancellationToken = default)
    {
        var assignment = new Assignment
        {
            CourseId = dto.CourseId,
            AssignmentNumber = dto.AssignmentNumber,
            CourseModuleId = dto.CourseModuleId,
            CourseLessonId = dto.CourseLessonId,
            Title = dto.Title,
            Instructions = dto.Instructions,
            MaxPoints = dto.MaxPoints > 0 ? dto.MaxPoints : 100m,
            DueDate = dto.DueDate,
            IsPublished = dto.IsPublished,
            CreatedByInstructorId = instructorId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Assignments.AddAsync(assignment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var enrollments = await _context.Enrollments
            .Where(e => e.CourseId == dto.CourseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var en in enrollments)
        {
            _context.Assessments.Add(new Assessment
            {
                StudentId = en.StudentId,
                CourseId = dto.CourseId,
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

        return assignment.Id;
    }

    public async Task<bool> UpdateAssignmentAsync(int id, UpdateAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.Assignments.FindAsync(new object[] { id }, cancellationToken);
        if (assignment == null || assignment.IsDeleted) return false;

        assignment.Title = dto.Title;
        assignment.Instructions = dto.Instructions;
        assignment.MaxPoints = dto.MaxPoints > 0 ? dto.MaxPoints : assignment.MaxPoints;
        assignment.DueDate = dto.DueDate;
        assignment.IsPublished = dto.IsPublished;
        assignment.UpdatedAt = DateTime.UtcNow;

        var assessments = await _context.Assessments.Where(a => a.AssignmentId == id).ToListAsync(cancellationToken);
        foreach (var ass in assessments)
        {
            ass.Title = dto.Title;
            ass.MaxScore = assignment.MaxPoints;
            ass.DueDate = assignment.DueDate;
            ass.IsPublished = assignment.IsPublished;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAssignmentAsync(int id, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.Assignments.FindAsync(new object[] { id }, cancellationToken);
        if (assignment == null || assignment.IsDeleted) return false;

        assignment.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(string? url, string? fileName)> UploadAssignmentAttachmentAsync(int id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.Assignments.FindAsync(new object[] { id }, cancellationToken);
        if (assignment == null || assignment.IsDeleted) return (null, null);

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "assignment_attachments");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var uniqueName = $"assign_{id}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"/uploads/assignment_attachments/{uniqueName}";
        assignment.AttachmentUrl = url;
        assignment.AttachmentFileName = file.FileName;
        await _context.SaveChangesAsync(cancellationToken);

        return (url, file.FileName);
    }

    public async Task<List<AssignmentSubmissionDto>> GetAssignmentSubmissionsAsync(int id, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        if (assignment == null) return new List<AssignmentSubmissionDto>();

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

        return submissions.Select(s => new AssignmentSubmissionDto
        {
            Id = s.Id,
            AssignmentId = s.AssignmentId,
            AssignmentTitle = assignment.Title,
            MaxPoints = assignment.MaxPoints,
            StudentId = s.StudentId,
            StudentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : "Student",
            StudentEmail = s.Student?.Email,
            StudentRegistrationNumber = s.Student?.RegistrationNumber,
            StudentAvatar = s.Student?.User?.ProfilePictureUrl ?? s.Student?.ProfilePictureUrl,
            SubmittedAt = s.SubmittedAt,
            Status = s.Status,
            ScoredPoints = s.ScoredPoints,
            Feedback = s.Feedback,
            SubmissionFileUrl = s.SubmissionFileUrl,
            SubmissionText = s.SubmissionText,
            GradedAt = s.GradedAt
        }).ToList();
    }

    public async Task<AssignmentSubmissionDto?> GetSubmissionDetailAsync(int submissionId, CancellationToken cancellationToken = default)
    {
        var s = await _context.Assessments
            .AsNoTracking()
            .Include(x => x.Assignment)
            .Include(x => x.Course)
            .Include(x => x.Student)
                .ThenInclude(st => st.User)
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        if (s == null) return null;

        return new AssignmentSubmissionDto
        {
            Id = s.Id,
            AssignmentId = s.AssignmentId,
            AssignmentTitle = s.Assignment?.Title ?? s.Title,
            AssignmentInstructions = s.Assignment?.Instructions,
            AssignmentAttachmentUrl = s.Assignment?.AttachmentUrl,
            MaxPoints = s.MaxScore,
            CourseId = s.CourseId,
            CourseTitle = s.Course?.Title,
            StudentId = s.StudentId,
            StudentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : "Student",
            StudentEmail = s.Student?.Email,
            StudentRegistrationNumber = s.Student?.RegistrationNumber,
            StudentAvatar = s.Student?.User?.ProfilePictureUrl ?? s.Student?.ProfilePictureUrl,
            SubmittedAt = s.SubmittedAt,
            DueDate = s.DueDate,
            Status = s.Status,
            ScoredPoints = s.ScoredPoints,
            Feedback = s.Feedback,
            RubricScoresJson = s.RubricScoresJson,
            SubmissionFileUrl = s.SubmissionFileUrl,
            SubmissionText = s.SubmissionText,
            GradedAt = s.GradedAt
        };
    }

    public async Task<bool> GradeSubmissionAsync(int submissionId, GradeSubmissionDto dto, CancellationToken cancellationToken = default)
    {
        var assessment = await _context.Assessments
            .Include(a => a.Student)
            .Include(a => a.Course)
            .Include(a => a.Assignment)
            .FirstOrDefaultAsync(a => a.Id == submissionId, cancellationToken);

        if (assessment == null) return false;

        assessment.ScoredPoints = dto.ScoredPoints;
        assessment.Feedback = dto.Feedback;
        assessment.RubricScoresJson = dto.RubricScoresJson;
        assessment.Status = "Graded";
        assessment.GradedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == assessment.CourseId && e.StudentId == assessment.StudentId && !e.IsDeleted, cancellationToken);

        if (enrollment != null)
        {
            var totalAssignmentsCount = await _context.Assignments
                .CountAsync(a => a.CourseId == assessment.CourseId && !a.IsDeleted && a.IsPublished, cancellationToken);

            var studentAssessments = await _context.Assessments
                .Where(a => a.CourseId == assessment.CourseId && a.StudentId == assessment.StudentId)
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

            await _certificateService.CheckAndIssueCertificateIfEligibleAsync(assessment.StudentId, assessment.CourseId, cancellationToken);
        }

        if (assessment.Student != null)
        {
            var title = $"Graded: {assessment.Title}";
            var message = $"Your assignment '{assessment.Title}' in {assessment.Course?.Title} was graded: {dto.ScoredPoints}/{assessment.MaxScore} pts.";
            await _hubContext.Clients.Group($"Student_{assessment.StudentId}").ReceiveNotification(title, message);
            await _hubContext.Clients.Group($"Student_{assessment.StudentId}").ReceiveGradePublished(new
            {
                assessmentId = assessment.Id,
                courseId = assessment.CourseId,
                scoredPoints = assessment.ScoredPoints,
                maxScore = assessment.MaxScore
            });
        }

        return true;
    }

    public async Task<List<object>> GetMyAssignmentsAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var activeEnrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId &&
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
            .Where(a => a.StudentId == studentId && enrolledCourseIds.Contains(a.CourseId))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        return assignments.Select(a => (object)new
        {
            id = a.Id,
            submissionId = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.Id,
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
            status = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.Status ?? "Pending",
            submittedAt = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.SubmittedAt,
            scoredPoints = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.ScoredPoints,
            feedback = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.Feedback,
            gradedAt = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.GradedAt,
            submissionFileUrl = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.SubmissionFileUrl,
            submissionFileName = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.SubmissionFileName,
            submissionFileType = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.SubmissionFileType,
            submissionFileSize = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.SubmissionFileSize,
            submissionText = studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.SubmissionText,
            isPastDue = now > a.DueDate,
            canSubmit = (studentSubmissions.FirstOrDefault(s => s.AssignmentId == a.Id)?.Status ?? "Pending") != "Graded" && now <= a.DueDate
        }).ToList();
    }

    public async Task<object?> GetStudentAssignmentDetailAsync(int assignmentId, int studentId, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.CourseModule)
            .Include(a => a.CourseLesson)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted && a.IsPublished, cancellationToken);

        if (assignment == null) return null;

        var isEnrolled = await _context.Enrollments.AnyAsync(e =>
            e.StudentId == studentId &&
            e.CourseId == assignment.CourseId &&
            e.Status != EnrollmentStatus.Dropped &&
            e.Status != EnrollmentStatus.Rejected &&
            !e.IsDeleted && !e.IsArchived, cancellationToken);

        if (!isEnrolled) return null;

        var sub = await _context.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId, cancellationToken);

        var now = DateTime.UtcNow;
        var status = sub?.Status ?? "Pending";
        var isPastDue = now > assignment.DueDate;
        var canSubmit = status != "Graded" && !isPastDue;

        return new
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
            status,
            submittedAt = sub?.SubmittedAt,
            scoredPoints = sub?.ScoredPoints,
            feedback = sub?.Feedback,
            gradedAt = sub?.GradedAt,
            submissionFileUrl = sub?.SubmissionFileUrl,
            submissionFileName = sub?.SubmissionFileName,
            submissionFileType = sub?.SubmissionFileType,
            submissionFileSize = sub?.SubmissionFileSize,
            submissionText = sub?.SubmissionText,
            isPastDue,
            canSubmit
        };
    }

    public async Task<object?> SubmitAssignmentAsync(int assignmentId, int studentId, SubmitAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.FindAsync(new object[] { studentId }, cancellationToken);
        if (student == null) return null;

        var assignment = await _context.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted && a.IsPublished, cancellationToken);

        if (assignment == null) return null;

        string? fileUrl = null;
        string? fileName = null;
        string? fileType = null;
        long? fileSize = null;

        if (dto.File != null && dto.File.Length > 0)
        {
            var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "submissions");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueName = $"sub_{studentId}_{assignment.Id}_{DateTime.UtcNow.Ticks}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream, cancellationToken);
            }

            fileUrl = $"/uploads/submissions/{uniqueName}";
            fileName = dto.File.FileName;
            fileType = ext;
            fileSize = dto.File.Length;
        }

        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.StudentId == studentId, cancellationToken);

        if (assessment != null)
        {
            if (assessment.Status == "Graded") return null;

            assessment.Title = assignment.Title;
            assessment.MaxScore = assignment.MaxPoints;
            assessment.DueDate = assignment.DueDate;
            assessment.SubmittedAt = DateTime.UtcNow;
            assessment.Status = "Submitted";
            if (!string.IsNullOrWhiteSpace(dto.SubmissionText)) assessment.SubmissionText = dto.SubmissionText;
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
            assessment = new Assessment
            {
                StudentId = studentId,
                CourseId = assignment.CourseId,
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                MaxScore = assignment.MaxPoints,
                DueDate = assignment.DueDate,
                SubmittedAt = DateTime.UtcNow,
                Status = "Submitted",
                IsPublished = true,
                SubmissionText = dto.SubmissionText,
                SubmissionFileUrl = fileUrl,
                SubmissionFileName = fileName,
                SubmissionFileType = fileType,
                SubmissionFileSize = fileSize
            };
            await _context.Assessments.AddAsync(assessment, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.ReceiveAssignmentUpdate(new
        {
            action = "submitted",
            assignmentId = assignment.Id,
            courseId = assignment.CourseId,
            studentId
        });

        await _hubContext.Clients.All.ReceiveEnrollmentNotification(new
        {
            action = "submission",
            studentId,
            courseId = assignment.CourseId
        });

        if (assignment.CreatedByInstructorId.HasValue)
        {
            await _hubContext.Clients.Group($"Instructor_{assignment.CreatedByInstructorId.Value}")
                .ReceiveNotification("New Student Submission", $"{student.FirstName} {student.LastName} submitted '{assignment.Title}'.");
        }

        return new
        {
            message = "Assignment submitted successfully.",
            submissionId = assessment.Id,
            assignmentId = assignment.Id,
            status = assessment.Status,
            submittedAt = assessment.SubmittedAt,
            fileName = assessment.SubmissionFileName,
            fileUrl = assessment.SubmissionFileUrl,
            submissionText = assessment.SubmissionText
        };
    }
}
