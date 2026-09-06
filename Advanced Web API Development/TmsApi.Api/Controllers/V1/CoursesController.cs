using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Application.Courses.Commands;
using TmsApi.Application.Courses.Queries;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Courses")]
public class CoursesController(
    IMediator _mediator,
    TmsDbContext? _context = null)
    : ControllerBase
{

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CourseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List courses with search, filters, and field shaping")]
    [EndpointDescription(
        "Returns a paged list of courses for any authenticated caller. Students are restricted to " +
        "active courses only. When the Fields query parameter is supplied, the response is shaped " +
        "to contain only the requested fields and is returned as a paged set of shaped objects."
    )]
    public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            request.Status = "Active";
        }

        if (!string.IsNullOrWhiteSpace(request.Fields))
        {
            var shapedResponse = await _mediator.Send(new GetShapedCoursesQuery(request), cancellationToken);
            return Ok(shapedResponse);
        }

        var response = await _mediator.Send(new GetCoursesQuery(request), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpGet("my-courses")]
    [ProducesResponseType(typeof(List<StudentEnrolledCourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get the authenticated student's enrolled courses")]
    [EndpointDescription(
        "Returns all courses the currently authenticated student is enrolled in with progress and " +
        "schedule information. Returns 403 Forbidden when the user is not linked to a student profile."
    )]
    public async Task<IActionResult> GetStudentMyCourses(CancellationToken cancellationToken)
    {
        if (_context == null) return BadRequest(new { message = "Database context not available." });
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        var courses = await _mediator.Send(new GetStudentEnrolledCoursesQuery(studentId.Value), cancellationToken);
        return Ok(courses);
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpGet("my-courses/statistics")]
    [ProducesResponseType(typeof(StudentCoursesStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get the authenticated student's course statistics")]
    [EndpointDescription(
        "Returns aggregate enrollment statistics for the currently authenticated student, including " +
        "total, active, completed, and upcoming course counts, total credits, and overall progress."
    )]
    public async Task<IActionResult> GetStudentMyCoursesStatistics(CancellationToken cancellationToken)
    {
        if (_context == null) return BadRequest(new { message = "Database context not available." });
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        var statistics = await _mediator.Send(new GetStudentCoursesStatisticsQuery(studentId.Value), cancellationToken);
        return Ok(statistics);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course by ID")]
    [EndpointDescription(
        "Returns the complete detail of a single course, including schedules, modules, lessons, " +
        "resources, and enrollment statistics. Returns 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken cancellationToken)
    {
        var course = await _mediator.Send(new GetCourseByIdQuery(id), cancellationToken);
        if (course == null) return NotFound(new { message = $"Course with ID {id} not found." });

        return Ok(course);
    }

    [HttpGet("catalog")]
    [ProducesResponseType(typeof(PagedResponse<CourseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Browse the public course catalog")]
    [EndpointDescription(
        "Returns a paged list of active courses intended for catalog browsing. The Status filter is " +
        "always forced to Active regardless of caller, so only currently available courses are returned."
    )]
    public async Task<IActionResult> GetCatalog([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        request.Status = "Active";
        var response = await _mediator.Send(new GetCoursesQuery(request), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a new course")]
    [EndpointDescription(
        "Creates a new course submitted by an instructor or administrator. The course is initially " +
        "created with the provided status and becomes available to students only after administrator " +
        "approval when required. Returns 201 Created with the new course."
    )]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto, CancellationToken cancellationToken)
    {
        var command = new CreateCourseCommand(dto);
        var created = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCourseById), new { version = "1.0", id = created.Id }, created);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update an existing course")]
    [EndpointDescription(
        "Updates the details of the specified course, including schedule, modules, and assessment " +
        "configuration. Persists changes to the database and returns 204 No Content on success, or " +
        "404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateCourseCommand(id, dto), cancellationToken);
        if (!updated) return NotFound(new { message = $"Course with ID {id} not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete a course")]
    [EndpointDescription(
        "Permanently deletes the specified course. Administrator-only operation. Returns 204 No " +
        "Content on success and 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteCourseCommand(id), cancellationToken);
        if (!deleted) return NotFound(new { message = $"Course with ID {id} not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Approve a course for publication")]
    [EndpointDescription(
        "Approves the specified course so it becomes active and available to students. Returns 400 " +
        "Bad Request when the course cannot be approved from its current state, and 404 Not Found " +
        "when the course does not exist."
    )]
    public async Task<IActionResult> ApproveCourse(int id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _mediator.Send(new ApproveCourseCommand(id), cancellationToken);
            if (!success) return NotFound(new { message = $"Course with ID {id} not found." });

            var course = await _mediator.Send(new GetCourseByIdQuery(id), cancellationToken);
            return Ok(new
            {
                message = "Course approved successfully.",
                courseId = id,
                status = "Active",
                course
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Reject a course with a reason")]
    [EndpointDescription(
        "Rejects the specified course and stores a rejection reason visible to the submitting " +
        "instructor. When no reason is provided, a default catalog-specification message is used. " +
        "Returns 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> RejectCourse(int id, [FromBody] RejectCourseDto dto, CancellationToken cancellationToken)
    {
        var reason = !string.IsNullOrWhiteSpace(dto?.Reason)
            ? dto.Reason.Trim()
            : "Course does not meet catalog specifications.";

        var success = await _mediator.Send(new RejectCourseCommand(id, reason), cancellationToken);
        if (!success) return NotFound(new { message = $"Course with ID {id} not found." });

        return Ok(new
        {
            message = "Course rejected.",
            courseId = id,
            status = "Rejected",
            rejectionReason = reason
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{id:int}/builder")]
    [ProducesResponseType(typeof(CourseBuilderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course's builder workspace")]
    [EndpointDescription(
        "Returns the authoring data for the course builder, including modules, lessons, resources, " +
        "assessment configuration, and schedule details. Intended for instructors and administrators " +
        "authoring course content. Returns 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> GetCourseBuilder(int id, CancellationToken cancellationToken)
    {
        var builder = await _mediator.Send(new GetCourseForBuilderQuery(id), cancellationToken);
        if (builder == null) return NotFound(new { message = "Course not found." });

        return Ok(builder);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/thumbnail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Upload a course thumbnail image")]
    [EndpointDescription(
        "Uploads a JPEG, PNG, or WebP thumbnail image (max 3MB) for the specified course and stores " +
        "the resulting image URL. Returns 400 Bad Request when no file is provided, the file exceeds " +
        "the size limit, or its content type is not allowed, and 404 Not Found when the course does " +
        "not exist."
    )]
    public async Task<IActionResult> UploadCourseThumbnail(int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
        if (file.Length > 3 * 1024 * 1024) return BadRequest(new { message = "Image exceeds 3MB limit." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Only JPEG, PNG, or WebP images are allowed." });

        var url = await _mediator.Send(new UploadCourseThumbnailCommand(id, file), cancellationToken);
        if (url == null) return NotFound(new { message = "Course not found." });

        return Ok(new { imageUrl = url });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/modules")]
    [ProducesResponseType(typeof(CourseModuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Add a module to a course")]
    [EndpointDescription(
        "Creates a new module within the specified course and returns the created module including " +
        "its auto-assigned order index. Returns 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> AddModule(int id, [FromBody] ModuleRequest request, CancellationToken cancellationToken)
    {
        var dto = new CreateModuleDto { Title = request.Title, Description = request.Description };
        var mod = await _mediator.Send(new AddModuleCommand(id, dto), cancellationToken);
        if (mod == null) return NotFound(new { message = "Course not found." });

        return Ok(new { id = mod.Id, title = mod.Title, description = mod.Description, orderIndex = mod.OrderIndex });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/modules/{moduleId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update an existing course module")]
    [EndpointDescription(
        "Updates the title and description of the specified module within the course. Returns 404 " +
        "Not Found when the module does not exist."
    )]
    public async Task<IActionResult> UpdateModule(int id, int moduleId, [FromBody] ModuleRequest request, CancellationToken cancellationToken)
    {
        var dto = new UpdateModuleDto { Title = request.Title, Description = request.Description };
        var success = await _mediator.Send(new UpdateModuleCommand(moduleId, dto), cancellationToken);
        if (!success) return NotFound(new { message = "Module not found." });

        return Ok(new { id = moduleId, title = request.Title, description = request.Description });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}/modules/{moduleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete a course module")]
    [EndpointDescription(
        "Permanently deletes the specified module and its lessons from the course. Returns 204 No " +
        "Content on success and 404 Not Found when the module does not exist."
    )]
    public async Task<IActionResult> DeleteModule(int id, int moduleId, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new DeleteModuleCommand(moduleId), cancellationToken);
        if (!success) return NotFound(new { message = "Module not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/modules/reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Reorder modules within a course")]
    [EndpointDescription(
        "Updates the display order indices for one or more modules within the specified course. " +
        "The request body contains the module IDs and their new order indices."
    )]
    public async Task<IActionResult> ReorderModules(int id, [FromBody] ReorderRequest request, CancellationToken cancellationToken)
    {
        var items = request.Items.Select(i => new ReorderItemDto { Id = i.Id, OrderIndex = i.OrderIndex }).ToList();
        var success = await _mediator.Send(new ReorderModulesCommand(id, items), cancellationToken);
        return Ok(new { message = "Modules reordered successfully." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/modules/{moduleId:int}/lessons")]
    [ProducesResponseType(typeof(CourseLessonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Add a lesson to a course module")]
    [EndpointDescription(
        "Creates a new lesson within the specified module and returns the created lesson with its " +
        "auto-assigned order index. Returns 404 Not Found when the module does not exist."
    )]
    public async Task<IActionResult> AddLesson(int id, int moduleId, [FromBody] LessonRequest request, CancellationToken cancellationToken)
    {
        var dto = new CreateLessonDto
        {
            Title = request.Title,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            ContentText = request.ContentText,
            PdfUrl = request.PdfUrl,
            PdfFileName = request.PdfFileName
        };

        var lesson = await _mediator.Send(new AddLessonCommand(moduleId, dto), cancellationToken);
        if (lesson == null) return NotFound(new { message = "Module not found." });

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update an existing lesson")]
    [EndpointDescription(
        "Updates the title, description, duration, content, and associated resources of the " +
        "specified lesson within the module. Returns 404 Not Found when the lesson does not exist."
    )]
    public async Task<IActionResult> UpdateLesson(int id, int moduleId, int lessonId, [FromBody] LessonRequest request, CancellationToken cancellationToken)
    {
        var dto = new UpdateLessonDto
        {
            Title = request.Title,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            ContentText = request.ContentText,
            PdfUrl = request.PdfUrl,
            PdfFileName = request.PdfFileName
        };

        var success = await _mediator.Send(new UpdateLessonCommand(lessonId, dto), cancellationToken);
        if (!success) return NotFound(new { message = "Lesson not found." });

        return Ok(new
        {
            id = lessonId,
            title = request.Title,
            description = request.Description,
            durationMinutes = request.DurationMinutes,
            contentText = request.ContentText,
            pdfUrl = request.PdfUrl,
            pdfFileName = request.PdfFileName
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}/modules/{moduleId:int}/lessons/{lessonId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete a lesson from a module")]
    [EndpointDescription(
        "Permanently deletes the specified lesson within the module. Returns 204 No Content on " +
        "success and 404 Not Found when the lesson does not exist."
    )]
    public async Task<IActionResult> DeleteLesson(int id, int moduleId, int lessonId, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new DeleteLessonCommand(lessonId), cancellationToken);
        if (!success) return NotFound(new { message = "Lesson not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/modules/{moduleId:int}/lessons/reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Reorder lessons within a module")]
    [EndpointDescription(
        "Updates the display order indices for one or more lessons within the specified module. " +
        "The request body contains the lesson IDs and their new order indices."
    )]
    public async Task<IActionResult> ReorderLessons(int id, int moduleId, [FromBody] ReorderRequest request, CancellationToken cancellationToken)
    {
        var items = request.Items.Select(i => new ReorderItemDto { Id = i.Id, OrderIndex = i.OrderIndex }).ToList();
        var success = await _mediator.Send(new ReorderLessonsCommand(moduleId, items), cancellationToken);
        return Ok(new { message = "Lessons reordered successfully." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/resources/upload")]
    [ProducesResponseType(typeof(CourseResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Upload a course resource file")]
    [EndpointDescription(
        "Uploads a resource file (up to 20MB) for the specified course and stores the file metadata " +
        "including title, type, and download URL. Returns 400 Bad Request when no file is provided " +
        "or the file exceeds the size limit, and 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> UploadResource(int id, IFormFile file, [FromForm] string? title, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
        if (file.Length > 20 * 1024 * 1024) return BadRequest(new { message = "File exceeds 20MB limit." });

        var resource = await _mediator.Send(new UploadCourseResourceCommand(id, title ?? file.FileName, file), cancellationToken);
        if (resource == null) return NotFound(new { message = "Course not found." });

        return Ok(resource);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}/resources/{resourceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete a course resource")]
    [EndpointDescription(
        "Permanently deletes the specified resource file from the course. Returns 204 No Content " +
        "on success and 404 Not Found when the resource does not exist."
    )]
    public async Task<IActionResult> DeleteResource(int id, int resourceId, CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new DeleteResourceCommand(resourceId), cancellationToken);
        if (!success) return NotFound(new { message = "Resource not found." });

        return NoContent();
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpGet("instructor-my-courses")]
    [ProducesResponseType(typeof(List<InstructorCourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get the authenticated instructor's assigned courses")]
    [EndpointDescription(
        "Returns all courses assigned to the currently authenticated instructor, including schedule " +
        "and enrollment information. Returns 403 Forbidden when the user is not linked to an " +
        "instructor profile."
    )]
    public async Task<IActionResult> GetCurrentInstructorMyCourses(CancellationToken cancellationToken)
    {
        if (_context == null) return BadRequest(new { message = "Database context not available." });
        var instructorId = await User.ResolveInstructorIdAsync(_context);
        if (instructorId == null)
        {
            return Forbid("Authenticated user is not linked to an instructor profile.");
        }

        var courses = await _mediator.Send(new GetInstructorMyCoursesQuery(instructorId.Value), cancellationToken);
        return Ok(courses);
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpGet("instructor/{instructorId:int}/my-courses")]
    [ProducesResponseType(typeof(List<InstructorCourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get a specific instructor's assigned courses")]
    [EndpointDescription(
        "Returns all courses assigned to the specified instructor. Accessible only to instructors " +
        "and administrators."
    )]
    public async Task<IActionResult> GetInstructorMyCourses(int instructorId, CancellationToken cancellationToken)
    {
        var courses = await _mediator.Send(new GetInstructorMyCoursesQuery(instructorId), cancellationToken);
        return Ok(courses);
    }

}
