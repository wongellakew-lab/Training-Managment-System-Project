using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Application.Assignments.Commands;
using TmsApi.Application.Assignments.Queries;
using TmsApi.Application.DTOs.AssignmentDtos;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Assignments")]
public class AssignmentsController(
    IMediator _mediator,
    TmsDbContext _context)
    : ControllerBase
{

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course's assessment configuration")]
    [EndpointDescription(
        "Returns the assessment configuration of the specified course, describing the expected " +
        "assignments and grading weights. Accessible to administrators and to the instructor who " +
        "owns the course. Returns 403 Forbidden when the caller is not the course owner and 404 " +
        "Not Found when the course does not exist."
    )]
    public async Task<IActionResult> GetCourseAssessmentConfig(int courseId, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(courseId)) return Forbid();

        var config = await _mediator.Send(new GetCourseAssessmentConfigQuery(courseId), cancellationToken);
        if (config == null) return NotFound(new { message = "Course not found." });

        return Ok(config);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("course/{courseId:int}")]
    [ProducesResponseType(typeof(List<AssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("List a course's assignments")]
    [EndpointDescription(
        "Returns all assignments for the specified course. Accessible to administrators and to the " +
        "instructor who owns the course. Returns 403 Forbidden when the caller is not authorized " +
        "to manage the course."
    )]
    public async Task<IActionResult> GetAssignmentsByCourse(int courseId, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(courseId)) return Forbid();

        var list = await _mediator.Send(new GetAssignmentsByCourseQuery(courseId), cancellationToken);
        return Ok(list);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an assignment by ID")]
    [EndpointDescription(
        "Returns a single assignment with submission statistics. Accessible to administrators and " +
        "to the instructor who owns the assignment's course. Returns 403 Forbidden when the caller " +
        "is not authorized and 404 Not Found when the assignment does not exist."
    )]
    public async Task<IActionResult> GetAssignmentById(int id, CancellationToken cancellationToken)
    {
        var assignment = await _mediator.Send(new GetAssignmentByIdQuery(id), cancellationToken);
        if (assignment == null) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        return Ok(assignment);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create an assignment for a course")]
    [EndpointDescription(
        "Creates a new assignment within the specified course, including its due date, maximum " +
        "points, and publication state. Returns 400 Bad Request when the assignment data is " +
        "invalid, for example when a previous assignment already exists for the same number. The " +
        "caller must be an administrator or the instructor who owns the course."
    )]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAsync(request.CourseId)) return Forbid();

        var instructorId = await User.ResolveInstructorIdAsync(_context);
        var dto = new CreateAssignmentDto
        {
            CourseId = request.CourseId,
            AssignmentNumber = request.AssignmentNumber,
            CourseModuleId = request.CourseModuleId,
            CourseLessonId = request.CourseLessonId,
            Title = request.Title,
            Instructions = request.Instructions,
            MaxPoints = request.MaxPoints,
            DueDate = request.DueDate,
            IsPublished = request.IsPublished
        };

        try
        {
            var id = await _mediator.Send(new CreateAssignmentCommand(dto, instructorId), cancellationToken);
            return Ok(new { id, message = "Assignment created successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update an assignment")]
    [EndpointDescription(
        "Updates the title, instructions, maximum points, due date, and publication state of the " +
        "specified assignment. Returns 404 Not Found when the assignment does not exist and 403 " +
        "Forbidden when the caller does not own the course."
    )]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _mediator.Send(new GetAssignmentByIdQuery(id), cancellationToken);
        if (assignment == null) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        var dto = new UpdateAssignmentDto
        {
            Title = request.Title,
            Instructions = request.Instructions,
            MaxPoints = request.MaxPoints,
            DueDate = request.DueDate,
            IsPublished = request.IsPublished
        };

        var updated = await _mediator.Send(new UpdateAssignmentCommand(id, dto), cancellationToken);
        if (!updated) return NotFound(new { message = "Assignment not found." });

        return Ok(new { message = "Assignment updated successfully." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete an assignment")]
    [EndpointDescription(
        "Permanently deletes the specified assignment. Returns 204 No Content on success, 403 " +
        "Forbidden when the caller does not own the course, and 404 Not Found when the assignment " +
        "does not exist."
    )]
    public async Task<IActionResult> DeleteAssignment(int id, CancellationToken cancellationToken)
    {
        var assignment = await _mediator.Send(new GetAssignmentByIdQuery(id), cancellationToken);
        if (assignment == null) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        var deleted = await _mediator.Send(new DeleteAssignmentCommand(id), cancellationToken);
        if (!deleted) return NotFound(new { message = "Assignment not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{id:int}/attachment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Upload an attachment to an assignment")]
    [EndpointDescription(
        "Uploads a file attachment for the specified assignment and stores the resulting URL and " +
        "file name. Returns 400 Bad Request when the file is invalid and 404 Not Found when the " +
        "assignment does not exist. The caller must be an administrator or the course owner."
    )]
    public async Task<IActionResult> UploadAssignmentAttachment(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var assignment = await _mediator.Send(new GetAssignmentByIdQuery(id), cancellationToken);
        if (assignment == null) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        try
        {
            var (url, fileName) = await _mediator.Send(new UploadAssignmentAttachmentCommand(id, file), cancellationToken);
            return Ok(new { attachmentUrl = url, attachmentFileName = fileName });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{id:int}/submissions")]
    [ProducesResponseType(typeof(List<AssignmentSubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("List all submissions for an assignment")]
    [EndpointDescription(
        "Returns all student submissions for the specified assignment, including student details, " +
        "submitted text and files, and grading information. Accessible to administrators and the " +
        "course owner."
    )]
    public async Task<IActionResult> GetAssignmentSubmissions(int id, CancellationToken cancellationToken)
    {
        var assignment = await _mediator.Send(new GetAssignmentByIdQuery(id), cancellationToken);
        if (assignment == null) return NotFound(new { message = "Assignment not found." });
        if (!await CanManageCourseAsync(assignment.CourseId)) return Forbid();

        var list = await _mediator.Send(new GetAssignmentSubmissionsQuery(id), cancellationToken);
        return Ok(list);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("submissions/{submissionId:int}")]
    [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a single submission's detail")]
    [EndpointDescription(
        "Returns the detailed record of a single assignment submission including the student, " +
        "submitted content, files, and current grading state. Accessible to administrators and the " +
        "course owner."
    )]
    public async Task<IActionResult> GetSubmissionDetail(int submissionId, CancellationToken cancellationToken)
    {
        var detail = await _mediator.Send(new GetSubmissionDetailQuery(submissionId), cancellationToken);
        if (detail == null) return NotFound(new { message = "Submission not found." });
        if (detail.CourseId.HasValue && !await CanManageCourseAsync(detail.CourseId.Value)) return Forbid();

        return Ok(detail);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("submissions/{submissionId:int}/grade")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Grade an assignment submission")]
    [EndpointDescription(
        "Scores a student's submission, records the instructor's feedback and rubric scores, and " +
        "marks the submission as Graded. The update is persisted to the database. Returns 404 Not " +
        "Found when the submission does not exist. The caller must be an administrator or the " +
        "course owner."
    )]
    public async Task<IActionResult> GradeSubmission(int submissionId, [FromBody] GradeSubmissionRequest request, CancellationToken cancellationToken)
    {
        var detail = await _mediator.Send(new GetSubmissionDetailQuery(submissionId), cancellationToken);
        if (detail == null) return NotFound(new { message = "Submission not found." });
        if (detail.CourseId.HasValue && !await CanManageCourseAsync(detail.CourseId.Value)) return Forbid();

        var dto = new GradeSubmissionDto
        {
            ScoredPoints = request.ScoredPoints,
            Feedback = request.Feedback,
            RubricScoresJson = request.RubricScoresJson
        };

        var success = await _mediator.Send(new GradeSubmissionCommand(submissionId, dto), cancellationToken);
        if (!success) return NotFound(new { message = "Submission not found." });

        return Ok(new
        {
            message = "Submission graded successfully.",
            status = "Graded",
            scoredPoints = request.ScoredPoints,
            gradedAt = DateTime.UtcNow
        });
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpGet("my-assignments")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get the authenticated student's assignments")]
    [EndpointDescription(
        "Returns all assignments for the courses the currently authenticated student is enrolled " +
        "in, with their submission and grading state. Returns 403 Forbidden when the user is not " +
        "linked to a student account."
    )]
    public async Task<IActionResult> GetMyAssignments(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue) return Forbid("User is not linked to a student account.");

        var list = await _mediator.Send(new GetMyAssignmentsQuery(studentId.Value), cancellationToken);
        return Ok(list);
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpGet("{id:int}/student-view")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an assignment in the student's view")]
    [EndpointDescription(
        "Returns the assignment detail for the currently authenticated student, including their own " +
        "submission and grade if one exists. Returns 403 Forbidden when the user is not linked to a " +
        "student account or is not enrolled in the assignment's course, and 404 Not Found when the " +
        "assignment does not exist."
    )]
    public async Task<IActionResult> GetStudentAssignmentDetail(int id, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue) return StatusCode(StatusCodes.Status403Forbidden, new { message = "User is not linked to a student account." });

        try
        {
            var result = await _mediator.Send(new GetStudentAssignmentDetailQuery(id, studentId.Value), cancellationToken);
            if (result == null) return NotFound(new { message = "Assignment not found." });
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Student,Admin")]
    [HttpPost("{id:int}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Submit an assignment for the authenticated student")]
    [EndpointDescription(
        "Submits the currently authenticated student's assignment with an uploaded file and/or " +
        "submission text. The submission is persisted to the database. Returns 400 Bad Request when " +
        "a submission file fails validation, 403 Forbidden when the user is not linked to a student " +
        "account or is not enrolled in the course, and 404 Not Found when the assignment does not exist."
    )]
    public async Task<IActionResult> SubmitAssignment(int id, [FromForm] IFormFile? file, [FromForm] string? submissionText, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue) return StatusCode(StatusCodes.Status403Forbidden, new { message = "User is not linked to a student account." });

        var dto = new SubmitAssignmentDto
        {
            File = file,
            SubmissionText = submissionText
        };

        try
        {
            var result = await _mediator.Send(new SubmitAssignmentCommand(id, studentId.Value, dto), cancellationToken);
            if (result == null) return NotFound(new { message = "Assignment not found." });
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = "Assignment submission validation failed.",
                errors = new { submission = new[] { ex.Message } }
            });
        }
    }
}
