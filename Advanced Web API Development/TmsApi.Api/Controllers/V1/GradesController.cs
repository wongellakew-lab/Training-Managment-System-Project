using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.GradeDtos;
using TmsApi.Application.Grades.Commands;
using TmsApi.Application.Grades.Queries;
using TmsApi.Application.Grading;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Gradebook")]
public class GradesController(
    IMediator _mediator,
    TmsDbContext? _context = null)
    : ControllerBase
{

    [Authorize]
    [HttpGet("student/{studentId:int}")]
    [ProducesResponseType(typeof(StudentAcademicRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a student's academic record")]
    [EndpointDescription(
        "Returns the complete academic record of the specified student, including credits, " +
        "cumulative GPA, academic standing, and per-course results. Students may only view their " +
        "own record and receive 403 Forbidden otherwise. Returns 404 Not Found when the student " +
        "does not exist."
    )]
    public async Task<IActionResult> GetStudentAcademicRecord(int studentId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var currentStudentId = _context != null ? await TmsApi.Api.Extensions.ClaimsExtensions.ResolveStudentIdAsync(User, _context) : null;
            if (!currentStudentId.HasValue || currentStudentId.Value != studentId)
            {
                return Forbid();
            }
        }

        var result = await _mediator.Send(new GetStudentAcademicRecordQuery(studentId), cancellationToken);
        if (result == null) return NotFound(new { message = $"Student with ID {studentId} not found." });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("my-grades")]
    [ProducesResponseType(typeof(StudentAcademicRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the authenticated student's academic record")]
    [EndpointDescription(
        "Returns the academic record of the currently authenticated student, including credits, " +
        "cumulative GPA, academic standing, and per-course results. Returns 404 Not Found when the " +
        "user is not linked to a student record."
    )]
    public async Task<IActionResult> GetMyGrades(CancellationToken cancellationToken)
    {
        var currentStudentId = _context != null ? await TmsApi.Api.Extensions.ClaimsExtensions.ResolveStudentIdAsync(User, _context) : null;
        if (!currentStudentId.HasValue)
        {
            return NotFound(new { message = "Student record not found for authenticated user." });
        }

        return await GetStudentAcademicRecord(currentStudentId.Value, cancellationToken);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("assign")]
    [HttpPost("")]
    [ProducesResponseType(typeof(GradeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Assign a grade to an enrollment")]
    [EndpointDescription(
        "Assigns a letter grade and/or numeric score to the specified enrollment, updates the " +
        "student's GPA, and persists the change. Returns 404 Not Found when the enrollment does " +
        "not exist."
    )]
    public async Task<IActionResult> AssignGrade([FromBody] AssignGradeInput dto, CancellationToken cancellationToken)
    {
        var command = new AssignGradeCommand(new AssignGradeRequestDto
        {
            EnrollmentId = dto.EnrollmentId,
            LetterGrade = dto.LetterGrade,
            NumericScore = dto.NumericScore,
            Grade = dto.Grade
        });

        var result = await _mediator.Send(command, cancellationToken);
        if (result == null) return NotFound(new { message = $"Enrollment with ID {dto.EnrollmentId} not found." });

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("bulk-assign")]
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(List<GradeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Assign grades to multiple enrollments")]
    [EndpointDescription(
        "Assigns grades to multiple enrollments in a single operation and returns the result for " +
        "each enrollment, including any updated GPA values."
    )]
    public async Task<IActionResult> BulkAssignGrades([FromBody] BulkAssignInput dto, CancellationToken cancellationToken)
    {
        var command = new BulkAssignGradesCommand(new BulkAssignGradesRequestDto
        {
            Grades = (dto.Grades ?? Enumerable.Empty<BulkGradeInputItem>()).Select(g => new BulkGradeInputItemDto
            {
                EnrollmentId = g.EnrollmentId,
                LetterGrade = g.LetterGrade,
                NumericScore = g.NumericScore,
                Grade = g.Grade
            }).ToList()
        });

        var results = await _mediator.Send(command, cancellationToken);
        return Ok(results);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("gradebook/course/{courseId:int}")]
    [ProducesResponseType(typeof(CourseGradebookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course's gradebook")]
    [EndpointDescription(
        "Returns the full gradebook for the specified course, including all assessment columns and " +
        "each enrolled student's scores. Instructors may only view gradebooks for courses they own " +
        "and receive 403 Forbidden otherwise. Returns 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> GetCourseGradebook(int courseId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            if (_context != null)
            {
                var instructorId = await TmsApi.Api.Extensions.ClaimsExtensions.ResolveInstructorIdAsync(User, _context);
                var courseOwner = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
                if (courseOwner == null || courseOwner.InstructorId != instructorId) return Forbid();
            }
        }

        var result = await _mediator.Send(new GetCourseGradebookQuery(courseId), cancellationToken);
        if (result == null) return NotFound(new { message = "Course not found." });

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("gradebook/bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Submit grades for multiple gradebook cells")]
    [EndpointDescription(
        "Updates the scores and feedback for many student/assessment cells of a course gradebook in " +
        "a single operation. Returns 400 Bad Request when no grades are submitted or a score fails " +
        "validation, and 403 Forbidden when an instructor submits for a course they do not own."
    )]
    public async Task<IActionResult> SubmitBulkGradebook([FromBody] BulkGradebookRequest request, CancellationToken cancellationToken)
    {
        if (request.Grades == null || !request.Grades.Any())
        {
            return BadRequest(new { message = "No grades submitted." });
        }

        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            if (_context != null)
            {
                var instructorId = await TmsApi.Api.Extensions.ClaimsExtensions.ResolveInstructorIdAsync(User, _context);
                var courseOwner = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);
                if (courseOwner == null || courseOwner.InstructorId != instructorId) return Forbid();
            }
        }

        var command = new SubmitBulkGradebookCommand(new BulkGradebookSubmitRequestDto
        {
            CourseId = request.CourseId,
            Grades = request.Grades.Select(g => new BulkGradebookItemInputDto
            {
                StudentId = g.StudentId,
                AssignmentId = g.AssignmentId,
                Score = g.Score,
                Feedback = g.Feedback
            }).ToList()
        });

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            if (result != null)
            {
                var successProp = result.GetType().GetProperty("success")?.GetValue(result);
                if (successProp is bool s && !s)
                {
                    var msgProp = result.GetType().GetProperty("message")?.GetValue(result)?.ToString() ?? "Grade validation failed.";
                    return BadRequest(new
                    {
                        message = msgProp,
                        errors = new { score = new[] { msgProp } }
                    });
                }
            }
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = "Grade validation failed.",
                errors = new { score = new[] { ex.Message } }
            });
        }
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("gradebook/cell")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update a single gradebook cell")]
    [EndpointDescription(
        "Updates the score and feedback for one student/assessment cell in a course gradebook. " +
        "Returns 400 Bad Request when the score fails validation, 403 Forbidden when an instructor " +
        "edits a course they do not own, and 404 Not Found when the assignment does not exist."
    )]
    public async Task<IActionResult> UpdateGradebookCell([FromBody] GradebookCellRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            if (_context != null)
            {
                var instructorId = await TmsApi.Api.Extensions.ClaimsExtensions.ResolveInstructorIdAsync(User, _context);
                var courseOwner = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);
                if (courseOwner == null || courseOwner.InstructorId != instructorId) return Forbid();
            }
        }

        var command = new UpdateGradebookCellCommand(new GradebookCellRequestDto
        {
            CourseId = request.CourseId,
            StudentId = request.StudentId,
            AssignmentId = request.AssignmentId,
            Score = request.Score,
            Feedback = request.Feedback
        });

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            if (result == null) return NotFound(new { message = "Assignment not found." });
            var successProp = result.GetType().GetProperty("success")?.GetValue(result);
            if (successProp is bool s && !s)
            {
                var msgProp = result.GetType().GetProperty("message")?.GetValue(result)?.ToString() ?? "Grade validation failed.";
                return BadRequest(new
                {
                    message = msgProp,
                    errors = new { score = new[] { msgProp } }
                });
            }
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = "Grade validation failed.",
                errors = new { score = new[] { ex.Message } }
            });
        }
    }

}
