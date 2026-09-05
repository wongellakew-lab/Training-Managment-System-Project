using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly TmsDbContext _context;
    private readonly IHubContext<TmsHub, ITmsClient> _hubContext;
    private readonly IWebHostEnvironment _env;
    private readonly IValidator<CreateCourseDto> _createValidator;
    private readonly IValidator<UpdateCourseDto> _updateValidator;
    private readonly INotificationService _notificationService;

    public CoursesController(
        ICourseService courseService,
        TmsDbContext context,
        IHubContext<TmsHub, ITmsClient> hubContext,
        IWebHostEnvironment env,
        IValidator<CreateCourseDto> createValidator,
        IValidator<UpdateCourseDto> updateValidator,
        INotificationService notificationService)
    {
        _courseService = courseService;
        _context = context;
        _hubContext = hubContext;
        _env = env;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            request.Status = "Active";
        }

        if (!string.IsNullOrWhiteSpace(request.Fields))
        {
            var shapedResponse = await _courseService.GetShapedCoursesAsync(request, cancellationToken);
            return Ok(shapedResponse);
        }

        var response = await _courseService.GetCoursesAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCourseByIdAsync(id, cancellationToken);
        if (course == null) return NotFound(new { message = $"Course with ID {id} not found." });

        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId.HasValue)
        {
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == studentId.Value && e.Status != Domain.Entities.EnrollmentStatus.Dropped && e.Status != Domain.Entities.EnrollmentStatus.Rejected, cancellationToken);
            if (enrollment != null)
            {
                course.IsStudentEnrolled = true;
                course.StudentEnrollmentStatus = enrollment.Status.ToString();

                int totalExpected = course.ExpectedAssignmentCount + course.ExpectedMidtermCount + course.ExpectedFinalCount;
                if (totalExpected == 0)
                {
                    int totalAssignments = await _context.Assignments
                        .CountAsync(a => a.CourseId == id && !a.IsDeleted && a.IsPublished, cancellationToken);
                    totalExpected = totalAssignments > 0 ? totalAssignments : 1;
                }

                int evaluatedAssessments = await _context.Assessments
                    .CountAsync(a => a.CourseId == id && a.StudentId == studentId.Value && a.Status == "Graded" && a.ScoredPoints.HasValue, cancellationToken);

                course.StudentProgress = enrollment.Status == Domain.Entities.EnrollmentStatus.Completed
                    ? 100m
                    : totalExpected > 0
                        ? Math.Min(100m, Math.Round((evaluatedAssessments / (decimal)totalExpected) * 100m, 1))
                        : 0m;
            }
        }

        return Ok(course);
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        // Enforce strictly that only Admin-approved "Active" courses appear in the student catalog
        request.Status = "Active";
        var response = await _courseService.GetCoursesAsync(request, cancellationToken);
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId.HasValue && response.Items != null && response.Items.Any())
        {
            var courseIds = response.Items.Select(c => c.Id).ToList();
            var studentEnrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId.Value && courseIds.Contains(e.CourseId) && e.Status != Domain.Entities.EnrollmentStatus.Dropped && e.Status != Domain.Entities.EnrollmentStatus.Rejected)
                .ToDictionaryAsync(e => e.CourseId, cancellationToken);

            var enrolledCourseIds = studentEnrollments.Keys.ToList();

            var completedCounts = await _context.Assessments
                .Where(a => enrolledCourseIds.Contains(a.CourseId) && a.StudentId == studentId.Value && a.Status == "Graded" && a.ScoredPoints.HasValue)
                .GroupBy(a => a.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);

            foreach (var item in response.Items)
            {
                if (studentEnrollments.TryGetValue(item.Id, out var enrollment))
                {
                    item.IsStudentEnrolled = true;
                    item.StudentEnrollmentStatus = enrollment.Status.ToString();

                    int total = item.TotalAssessments > 0 ? item.TotalAssessments : 1;
                    int evaluated = completedCounts.TryGetValue(item.Id, out var c) ? c : 0;

                    item.StudentProgress = enrollment.Status == Domain.Entities.EnrollmentStatus.Completed
                        ? 100m
                        : Math.Min(100m, Math.Round((evaluated / (decimal)total) * 100m, 1));
                }
            }
        }
        return Ok(response);
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpGet("my-courses")]
    public async Task<IActionResult> GetMyCourses(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null) return Forbid("Authenticated user is not linked to a student profile.");

        var courses = await _courseService.GetStudentEnrolledCoursesAsync(studentId.Value, cancellationToken);
        return Ok(courses);
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpGet("my-courses/statistics")]
    public async Task<IActionResult> GetMyCoursesStatistics(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null) return Forbid("Authenticated user is not linked to a student profile.");

        var stats = await _courseService.GetStudentCoursesStatisticsAsync(studentId.Value, cancellationToken);
        return Ok(stats);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            var instructorId = await User.ResolveInstructorIdAsync(_context);
            if (!instructorId.HasValue) return Forbid("Authenticated user is not linked to an instructor profile.");
            dto.InstructorId = instructorId.Value;

            if (dto.Status == "Active" || dto.Status == "Published")
            {
                dto.Status = "Under Review";
            }
        }

        // Auto-generate code if not provided
        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            var prefix = !string.IsNullOrWhiteSpace(dto.Category) && dto.Category.Length >= 3
                ? dto.Category.Substring(0, 3).ToUpperInvariant()
                : "CRS";
            dto.Code = $"{prefix}-{Random.Shared.Next(100, 999)}";
        }
        else
        {
            var codeConflict = await _context.Courses
                .AnyAsync(c => c.Code.ToLower() == dto.Code.Trim().ToLower() && !c.IsDeleted, cancellationToken);
            if (codeConflict)
            {
                return BadRequest(new
                {
                    message = "Course code already exists.",
                    errors = new Dictionary<string, string[]>
                    {
                        ["code"] = new[] { "Course code already exists." }
                    }
                });
            }
        }

        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                message = "Course validation failed.",
                errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => string.IsNullOrEmpty(g.Key) ? g.Key : char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            });
        }

        var course = await _courseService.CreateCourseAsync(dto, cancellationToken);
        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "courseCreated", courseId = course.Id });

        string createMsg = course.Status == "Under Review"
            ? "Course submitted successfully. Your course is now under review."
            : (course.Status == "Active" ? "Course published successfully!" : "Course draft saved successfully!");

        return CreatedAtAction(nameof(GetCourseById), new { version = "1.0", id = course.Id }, new
        {
            id = course.Id,
            code = course.Code,
            title = course.Title,
            category = course.Category,
            level = course.Level,
            status = course.Status,
            durationHours = course.DurationHours,
            startDate = course.StartDate,
            endDate = course.EndDate,
            message = createMsg
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            var instructorId = await User.ResolveInstructorIdAsync(_context);
            if (!instructorId.HasValue) return Forbid("Authenticated user is not linked to an instructor profile.");

            var existingAuth = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existingAuth == null) return NotFound(new { message = $"Course with ID {id} not found." });
            if (existingAuth.InstructorId != instructorId.Value) return Forbid("You can only edit courses assigned to you.");

            // Always enforce instructor ownership on update
            dto.InstructorId = existingAuth.InstructorId ?? instructorId.Value;

            // Instructors cannot directly publish to Active
            if (dto.Status == "Active" || dto.Status == "Published")
            {
                dto.Status = "Under Review";
            }
        }
        else if (!dto.InstructorId.HasValue)
        {
            var existingCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existingCourse != null)
            {
                dto.InstructorId = existingCourse.InstructorId;
            }
        }

        var existing = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (existing == null) return NotFound(new { message = $"Course with ID {id} not found." });

        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            dto.Code = existing.Code;
        }

        // Code uniqueness check if modified
        if (!string.IsNullOrWhiteSpace(dto.Code) && !string.Equals(dto.Code.Trim(), existing.Code, StringComparison.OrdinalIgnoreCase))
        {
            var codeConflict = await _context.Courses
                .AnyAsync(c => c.Id != id && c.Code.ToLower() == dto.Code.Trim().ToLower() && !c.IsDeleted, cancellationToken);
            if (codeConflict)
            {
                return BadRequest(new
                {
                    message = "Course code already exists.",
                    errors = new Dictionary<string, string[]>
                    {
                        ["code"] = new[] { "Course code already exists." }
                    }
                });
            }
        }

        // Authoritative Publish/Submit check: Course Content must not be empty
        if (dto.Status == "Active" || dto.Status == "Published" || dto.Status == "Under Review")
        {
            bool hasContent = (dto.Modules != null && dto.Modules.Count > 0 && dto.Modules.Any(m => m.Lessons != null && m.Lessons.Count > 0))
                || (existing.Modules != null && existing.Modules.Count > 0 && existing.Modules.Any(m => m.Lessons != null && m.Lessons.Count > 0));

            if (!hasContent)
            {
                return BadRequest(new
                {
                    message = "Course content is required. Add at least one content item before publishing.",
                    errors = new Dictionary<string, string[]>
                    {
                        ["courseContent"] = new[] { "Course content is required. Add at least one content item before publishing." }
                    }
                });
            }
        }

        // Integrity check: Cannot reduce expected assignment count below already-created assignments
        var createdAssignmentsCount = await _context.Assignments
            .CountAsync(a => a.CourseId == id && !a.IsDeleted, cancellationToken);

        if (dto.ExpectedAssignmentCount < createdAssignmentsCount)
        {
            return BadRequest(new
            {
                message = $"Cannot reduce configured assignment count to {dto.ExpectedAssignmentCount}. There are already {createdAssignmentsCount} assignments created for this course.",
                errors = new Dictionary<string, string[]>
                {
                    ["expectedAssignmentCount"] = new[] { $"Cannot reduce assignment count below {createdAssignmentsCount}, as {createdAssignmentsCount} assignments have already been created." }
                }
            });
        }

        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                message = "Course validation failed.",
                errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => string.IsNullOrEmpty(g.Key) ? g.Key : char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            });
        }

        var updated = await _courseService.UpdateCourseAsync(id, dto, cancellationToken);
        if (!updated) return NotFound(new { message = $"Course with ID {id} not found." });

        var reloaded = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "courseUpdated", courseId = id });

        string currentStatus = reloaded?.Status ?? dto.Status;
        string returnMsg = currentStatus == "Under Review"
            ? "Course submitted successfully. Your course is now under review."
            : (currentStatus == "Active" || currentStatus == "Published" ? "Course published successfully!" : "Course draft saved successfully!");

        return Ok(new
        {
            message = returnMsg,
            courseId = id,
            id = id,
            code = reloaded?.Code ?? dto.Code,
            title = reloaded?.Title ?? dto.Title,
            category = reloaded?.Category ?? dto.Category,
            level = reloaded?.Level ?? dto.Level,
            status = currentStatus,
            durationHours = reloaded?.DurationHours ?? dto.DurationHours,
            startDate = reloaded?.StartDate,
            endDate = reloaded?.EndDate
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken cancellationToken)
    {
        var deleted = await _courseService.DeleteCourseAsync(id, cancellationToken);
        if (!deleted) return NotFound(new { message = $"Course with ID {id} not found." });

        return NoContent();
    }

    // ─── Instructor Course Builder Endpoints ─────────────────────────────────

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("instructor-my-courses")]
    public async Task<IActionResult> GetInstructorMyCourses(CancellationToken cancellationToken)
    {
        var instructorId = await User.ResolveInstructorIdAsync(_context);
        if (!instructorId.HasValue) return Forbid("User is not linked to an instructor profile.");

        var courses = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .Include(c => c.Resources)
            .Include(c => c.Enrollments)
            .Include(c => c.Schedules)
            .Where(c => c.InstructorId == instructorId.Value && !c.IsDeleted)
            .OrderByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        var dtos = courses.Select(c => new
        {
            id = c.Id,
            code = c.Code,
            title = c.Title,
            category = c.Category,
            level = c.Level,
            status = c.Status,
            rejectionReason = c.RejectionReason,
            durationHours = c.DurationHours,
            lessonsCount = c.Modules.SelectMany(m => m.Lessons).Count(),
            modulesCount = c.Modules.Count,
            resourcesCount = c.Resources.Count,
            schedulesCount = c.Schedules.Count(s => !s.IsDeleted),
            activeStudentsCount = c.Enrollments.Count(e => e.Status == Domain.Entities.EnrollmentStatus.Active || e.Status == Domain.Entities.EnrollmentStatus.Completed),
            imageUrl = c.ImageUrl,
            about = c.About,
            description = c.Description,
            startDate = c.StartDate,
            endDate = c.EndDate
        }).ToList();

        return Ok(dtos);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{id:int}/builder")]
    public async Task<IActionResult> GetCourseForBuilder(int id, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Schedules)
            .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
            .Include(c => c.Resources)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (course == null) return NotFound(new { message = $"Course with ID {id} not found." });

        return Ok(new
        {
            id = course.Id,
            code = course.Code,
            title = course.Title,
            description = course.Description,
            about = course.About,
            category = course.Category,
            level = course.Level,
            status = course.Status,
            rejectionReason = course.RejectionReason,
            durationHours = course.DurationHours,
            lessonsCount = course.Modules.SelectMany(m => m.Lessons).Count(),
            imageUrl = course.ImageUrl,
            department = course.Department,
            featuresJson = course.FeaturesJson,
            instructorId = course.InstructorId,
            instructorName = course.Instructor != null ? $"{course.Instructor.FirstName} {course.Instructor.LastName}" : "Instructor",
            startDate = course.StartDate,
            endDate = course.EndDate,
            maxCapacity = course.MaxCapacity,
            expectedAssignmentCount = course.ExpectedAssignmentCount,
            expectedMidtermCount = course.ExpectedMidtermCount,
            expectedFinalCount = course.ExpectedFinalCount,
            totalAssessments = course.ExpectedAssignmentCount + course.ExpectedMidtermCount + course.ExpectedFinalCount,
            createdAssignmentsCount = _context.Assignments.Count(a => a.CourseId == course.Id && !a.IsDeleted),
            schedules = course.Schedules.Where(s => !s.IsDeleted).Select(s => new
            {
                id = s.Id,
                dayOfWeek = (int)s.DayOfWeek,
                dayName = s.DayOfWeek.ToString(),
                startTime = s.StartTime,
                endTime = s.EndTime,
                location = s.Location,
                room = s.Room,
                scheduleType = s.ScheduleType
            }).ToList(),
            modules = course.Modules.OrderBy(m => m.OrderIndex).Select(m => new
            {
                id = m.Id,
                title = m.Title,
                description = m.Description,
                orderIndex = m.OrderIndex,
                lessons = m.Lessons.OrderBy(l => l.OrderIndex).Select(l => new
                {
                    id = l.Id,
                    title = l.Title,
                    description = l.Description,
                    durationMinutes = l.DurationMinutes,
                    contentText = l.ContentText,
                    pdfUrl = l.PdfUrl,
                    pdfFileName = l.PdfFileName,
                    orderIndex = l.OrderIndex
                }).ToList()
            }).ToList(),
            resources = course.Resources.Select(r => new
            {
                id = r.Id,
                title = r.Title,
                fileType = r.FileType,
                fileSize = r.FileSize,
                url = r.Url
            }).ToList()
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> ApproveCourse(int id, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .Include(c => c.Instructor)
                .ThenInclude(i => i!.User)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (course == null) return NotFound(new { message = $"Course with ID {id} not found." });

        bool hasContent = course.Modules.Any(m => m.Lessons.Any());
        if (!hasContent)
        {
            return BadRequest(new { message = "Course cannot be approved because it has no modules or lessons." });
        }

        if (!course.Schedules.Any(s => !s.IsDeleted))
        {
            return BadRequest(new { message = "Course cannot be approved because it has no scheduled classes." });
        }

        course.Status = "Active";
        course.RejectionReason = null;
        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "courseApproved", courseId = id });

        if (course.Instructor != null && !string.IsNullOrEmpty(course.Instructor.UserId))
        {
            await _notificationService.CreateNotificationAsync(
                course.Instructor.UserId,
                "Course Approved",
                $"Your course '{course.Title}' ({course.Code}) has been approved by administration and is now active in the catalog.",
                "Course",
                course.Id,
                "Course",
                cancellationToken);
        }

        return Ok(new
        {
            message = "Course approved successfully.",
            courseId = id,
            status = "Active",
            course = await _courseService.GetCourseByIdAsync(id, cancellationToken)
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> RejectCourse(int id, [FromBody] RejectCourseDto dto, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .Include(c => c.Instructor)
                .ThenInclude(i => i!.User)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (course == null) return NotFound(new { message = $"Course with ID {id} not found." });

        var reason = !string.IsNullOrWhiteSpace(dto?.Reason)
            ? dto.Reason.Trim()
            : "Course does not meet catalog specifications.";

        course.Status = "Rejected";
        course.RejectionReason = reason;
        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "courseRejected", courseId = id });

        if (course.Instructor != null && !string.IsNullOrEmpty(course.Instructor.UserId))
        {
            await _notificationService.CreateNotificationAsync(
                course.Instructor.UserId,
                "Course Rejected",
                $"Your course '{course.Title}' ({course.Code}) was rejected. Reason: {reason}",
                "Course",
                course.Id,
                "Course",
                cancellationToken);
        }

        return Ok(new
        {
            message = "Course rejected.",
            courseId = id,
            status = "Rejected",
            rejectionReason = reason
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/thumbnail")]
    public async Task<IActionResult> UploadCourseThumbnail(int id, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
        if (file.Length > 3 * 1024 * 1024) return BadRequest(new { message = "Image exceeds 3MB limit." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Only JPEG, PNG, or WebP images are allowed." });

        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound(new { message = "Course not found." });

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "course_thumbnails");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var uniqueName = $"course_{id}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/course_thumbnails/{uniqueName}";
        course.ImageUrl = url;
        await _context.SaveChangesAsync();

        return Ok(new { imageUrl = url });
    }

    public record ModuleRequest(string Title, string? Description);
    public record ReorderItem(int Id, int OrderIndex);
    public record ReorderRequest(List<ReorderItem> Items);

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/modules")]
    public async Task<IActionResult> AddModule(int id, [FromBody] ModuleRequest request)
    {
        var course = await _context.Courses.Include(c => c.Modules).FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound(new { message = "Course not found." });

        var nextIndex = course.Modules.Any() ? course.Modules.Max(m => m.OrderIndex) + 1 : 1;
        var mod = new CourseModule
        {
            CourseId = id,
            Title = request.Title,
            Description = request.Description,
            OrderIndex = nextIndex
        };

        await _context.CourseModules.AddAsync(mod);
        await _context.SaveChangesAsync();

        return Ok(new { id = mod.Id, title = mod.Title, description = mod.Description, orderIndex = mod.OrderIndex });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/modules/{moduleId:int}")]
    public async Task<IActionResult> UpdateModule(int id, int moduleId, [FromBody] ModuleRequest request)
    {
        var mod = await _context.CourseModules.FirstOrDefaultAsync(m => m.Id == moduleId && m.CourseId == id);
        if (mod == null) return NotFound(new { message = "Module not found." });

        mod.Title = request.Title;
        mod.Description = request.Description;
        await _context.SaveChangesAsync();

        return Ok(new { id = mod.Id, title = mod.Title, description = mod.Description, orderIndex = mod.OrderIndex });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}/modules/{moduleId:int}")]
    public async Task<IActionResult> DeleteModule(int id, int moduleId)
    {
        var mod = await _context.CourseModules.Include(m => m.Lessons).FirstOrDefaultAsync(m => m.Id == moduleId && m.CourseId == id);
        if (mod == null) return NotFound(new { message = "Module not found." });

        _context.CourseModules.Remove(mod);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/modules/reorder")]
    public async Task<IActionResult> ReorderModules(int id, [FromBody] ReorderRequest request)
    {
        var modules = await _context.CourseModules.Where(m => m.CourseId == id).ToListAsync();
        foreach (var item in request.Items)
        {
            var mod = modules.FirstOrDefault(m => m.Id == item.Id);
            if (mod != null) mod.OrderIndex = item.OrderIndex;
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Modules reordered successfully." });
    }

    public record LessonRequest(string Title, string? Description, int DurationMinutes, string? ContentText, string? PdfUrl, string? PdfFileName);

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/modules/{moduleId:int}/lessons")]
    public async Task<IActionResult> AddLesson(int id, int moduleId, [FromBody] LessonRequest request)
    {
        var mod = await _context.CourseModules.Include(m => m.Lessons).FirstOrDefaultAsync(m => m.Id == moduleId && m.CourseId == id);
        if (mod == null) return NotFound(new { message = "Module not found." });

        var nextIndex = mod.Lessons.Any() ? mod.Lessons.Max(l => l.OrderIndex) + 1 : 1;
        var lesson = new CourseLesson
        {
            CourseModuleId = moduleId,
            Title = request.Title,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 45,
            ContentText = request.ContentText,
            PdfUrl = request.PdfUrl,
            PdfFileName = request.PdfFileName,
            OrderIndex = nextIndex
        };

        await _context.CourseLessons.AddAsync(lesson);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = lesson.Id,
            title = lesson.Title,
            description = lesson.Description,
            durationMinutes = lesson.DurationMinutes,
            contentText = lesson.ContentText,
            pdfUrl = lesson.PdfUrl,
            pdfFileName = lesson.PdfFileName,
            orderIndex = lesson.OrderIndex
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/modules/{moduleId:int}/lessons/{lessonId:int}")]
    public async Task<IActionResult> UpdateLesson(int id, int moduleId, int lessonId, [FromBody] LessonRequest request)
    {
        var lesson = await _context.CourseLessons.FirstOrDefaultAsync(l => l.Id == lessonId && l.CourseModuleId == moduleId);
        if (lesson == null) return NotFound(new { message = "Lesson not found." });

        lesson.Title = request.Title;
        lesson.Description = request.Description;
        lesson.DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : lesson.DurationMinutes;
        lesson.ContentText = request.ContentText;
        lesson.PdfUrl = request.PdfUrl;
        lesson.PdfFileName = request.PdfFileName;

        await _context.SaveChangesAsync();
        return Ok(new
        {
            id = lesson.Id,
            title = lesson.Title,
            description = lesson.Description,
            durationMinutes = lesson.DurationMinutes,
            contentText = lesson.ContentText,
            pdfUrl = lesson.PdfUrl,
            pdfFileName = lesson.PdfFileName,
            orderIndex = lesson.OrderIndex
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}/modules/{moduleId:int}/lessons/{lessonId:int}")]
    public async Task<IActionResult> DeleteLesson(int id, int moduleId, int lessonId)
    {
        var lesson = await _context.CourseLessons.FirstOrDefaultAsync(l => l.Id == lessonId && l.CourseModuleId == moduleId);
        if (lesson == null) return NotFound(new { message = "Lesson not found." });

        _context.CourseLessons.Remove(lesson);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/modules/{moduleId:int}/lessons/reorder")]
    public async Task<IActionResult> ReorderLessons(int id, int moduleId, [FromBody] ReorderRequest request)
    {
        var lessons = await _context.CourseLessons.Where(l => l.CourseModuleId == moduleId).ToListAsync();
        foreach (var item in request.Items)
        {
            var l = lessons.FirstOrDefault(x => x.Id == item.Id);
            if (l != null) l.OrderIndex = item.OrderIndex;
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Lessons reordered successfully." });
    }

    // ─── PDF / Course Resource Uploads ────────────────────────────────────────

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/resources/upload")]
    public async Task<IActionResult> UploadResource(int id, IFormFile file, [FromForm] string? title)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
        if (file.Length > 20 * 1024 * 1024) return BadRequest(new { message = "File exceeds 20MB limit." });

        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound(new { message = "Course not found." });

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "course_resources");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var uniqueName = $"res_{id}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var sizeMb = file.Length / (1024f * 1024f);
        var sizeStr = sizeMb >= 1.0f ? $"{sizeMb:F1} MB" : $"{file.Length / 1024f:F0} KB";

        var resource = new CourseResource
        {
            CourseId = id,
            Title = !string.IsNullOrWhiteSpace(title) ? title : file.FileName,
            FileType = ext.TrimStart('.').ToUpper(),
            FileSize = sizeStr,
            Url = $"/uploads/course_resources/{uniqueName}"
        };

        await _context.CourseResources.AddAsync(resource);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = resource.Id,
            title = resource.Title,
            fileType = resource.FileType,
            fileSize = resource.FileSize,
            url = resource.Url
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}/resources/{resourceId:int}")]
    public async Task<IActionResult> DeleteResource(int id, int resourceId)
    {
        var resource = await _context.CourseResources.FirstOrDefaultAsync(r => r.Id == resourceId && r.CourseId == id);
        if (resource == null) return NotFound(new { message = "Resource not found." });

        if (!string.IsNullOrEmpty(resource.Url))
        {
            var fileName = Path.GetFileName(resource.Url);
            var filePath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "course_resources", fileName);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        _context.CourseResources.Remove(resource);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
