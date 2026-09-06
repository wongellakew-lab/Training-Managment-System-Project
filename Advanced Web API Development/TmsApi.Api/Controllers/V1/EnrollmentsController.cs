using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Enrollments")]
public class EnrollmentsController(
    IMediator _mediator,
    TmsDbContext _context)
    : ControllerBase
{
    private async Task<int?> GetCurrentStudentIdAsync()
    {
        return await User.ResolveStudentIdAsync(_context);
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("List enrollments with filters and paging")]
    [EndpointDescription(
        "Returns a paged list of enrollments, optionally filtered by student, course, status, " +
        "category, and full-text search. Students are restricted to their own enrollments only. " +
        "Any authenticated user may call this endpoint."
    )]
    public async Task<IActionResult> GetEnrollments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? studentId = null,
        [FromQuery] int? courseId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var currentStudentId = await GetCurrentStudentIdAsync();
            if (currentStudentId.HasValue)
                studentId = currentStudentId.Value;
        }

        var response = await _mediator.Send(new GetEnrollmentsQuery(page, pageSize, studentId, courseId, search, status, category), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an enrollment by ID")]
    [EndpointDescription(
        "Returns a single enrollment. Students may only view their own enrollments and receive 403 " +
        "Forbidden otherwise; administrators and instructors may view any enrollment. Returns 404 " +
        "Not Found when the enrollment does not exist."
    )]
    public async Task<IActionResult> GetEnrollmentById(int id, CancellationToken cancellationToken)
    {
        var enrollment = await _mediator.Send(new GetEnrollmentByIdQuery(id), cancellationToken);
        if (enrollment == null) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var currentStudentId = await GetCurrentStudentIdAsync();
            if (currentStudentId.HasValue && enrollment.StudentId != currentStudentId.Value)
                return Forbid();
        }

        return Ok(enrollment);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Enroll a student in a course")]
    [EndpointDescription(
        "Enrolls a student in the specified course. When called by a student, the enrollment is " +
        "created for the currently authenticated student and any supplied student ID is ignored. " +
        "Returns 400 Bad Request when the course is full, unavailable, or the student already has " +
        "an active enrollment in that course."
    )]
    public async Task<IActionResult> EnrollStudent([FromBody] CreateEnrollmentDto dto, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin"))
        {
            var currentStudentId = await GetCurrentStudentIdAsync();
            if (!currentStudentId.HasValue)
                return Unauthorized(new { message = "Student profile not found for authenticated user." });
            dto.StudentId = currentStudentId.Value;
        }

        try
        {
            var created = await _mediator.Send(new EnrollStudentCommand(dto.StudentId, dto.CourseId), cancellationToken);
            return CreatedAtAction(nameof(GetEnrollmentById), new { version = "1.0", id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update an enrollment's status")]
    [EndpointDescription(
        "Sets the status of the specified enrollment to the value provided in the request body. " +
        "Persists the change to the database. Returns 404 Not Found when the enrollment does not exist."
    )]
    public async Task<IActionResult> UpdateEnrollmentStatus(int id, [FromBody] UpdateEnrollmentStatusDto dto, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateEnrollmentStatusCommand(id, dto.Status), cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return Ok(new { message = $"Enrollment status updated to {dto.Status}." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Approve a pending enrollment")]
    [EndpointDescription(
        "Approves the specified enrollment by setting its status to Active, which grants the " +
        "student access to the course. Returns 404 Not Found when the enrollment does not exist."
    )]
    public async Task<IActionResult> ApproveEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateEnrollmentStatusCommand(id, EnrollmentStatus.Active), cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return Ok(new { message = "Enrollment approved successfully." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Reject a pending enrollment")]
    [EndpointDescription(
        "Rejects the specified enrollment by setting its status to Rejected. Returns 404 Not Found " +
        "when the enrollment does not exist."
    )]
    public async Task<IActionResult> RejectEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateEnrollmentStatusCommand(id, EnrollmentStatus.Rejected), cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return Ok(new { message = "Enrollment rejected." });
    }

    [Authorize]
    [HttpPut("{id:int}/drop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Drop a course enrollment")]
    [EndpointDescription(
        "Drops the specified enrollment by setting its status to Dropped. Typically used by a " +
        "student to drop their own course enrollment. Returns 404 Not Found when the enrollment " +
        "does not exist."
    )]
    public async Task<IActionResult> DropEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateEnrollmentStatusCommand(id, EnrollmentStatus.Dropped), cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return Ok(new { message = "Course enrollment dropped." });
    }
}
