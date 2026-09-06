using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Students.Commands;
using TmsApi.Application.Students.Queries;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Students")]
public class StudentsController(
    IMediator _mediator,
    TmsDbContext _context,
    UserManager<TmsUser> _userManager) : ControllerBase
{

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("List students with paging, search, and filters")]
    [EndpointDescription(
        "Returns a paged list of students for administrators and instructors. Supports paging, " +
        "full-text search, and additional filter fields through the PagedRequest query parameters. " +
        "Accessible only to users with the Admin or Instructor role."
    )]
    public async Task<IActionResult> GetStudents([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetStudentsQuery(request), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("stats")]
    [ProducesResponseType(typeof(StudentStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get aggregate student statistics")]
    [EndpointDescription(
        "Returns high-level statistics about the student body, such as the total number of students, " +
        "actively enrolled students, and issued certificates. Intended for administrators and instructors."
    )]
    public async Task<IActionResult> GetStudentStats(CancellationToken cancellationToken)
    {
        var stats = await _mediator.Send(new GetStudentStatsQuery(), cancellationToken);
        return Ok(stats);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a student by ID")]
    [EndpointDescription(
        "Returns the profile of a single student. Students may only view their own profile; " +
        "administrators and instructors may view any student. Returns 403 Forbidden when a student " +
        "attempts to access another student's profile and 404 Not Found when the ID does not exist."
    )]
    public async Task<IActionResult> GetStudentById(int id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var currentStudentId = await User.ResolveStudentIdAsync(_context);
            if (!currentStudentId.HasValue || currentStudentId.Value != id)
            {
                return Forbid();
            }
        }

        var student = await _mediator.Send(new GetStudentByIdQuery(id), cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        return Ok(student);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a new student record")]
    [EndpointDescription(
        "Creates a new student record for an administrator. Returns 201 Created with the newly " +
        "created student, or 400 Bad Request when the provided data is invalid (for example a " +
        "duplicate registration number or email)."
    )]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto, CancellationToken cancellationToken)
    {
        var student = await _mediator.Send(new CreateStudentCommand(dto), cancellationToken);
        return CreatedAtAction(nameof(GetStudentById), new { version = "1.0", id = student.Id }, student);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update an existing student record")]
    [EndpointDescription(
        "Updates the personal details of the specified student. Persists the changes to the " +
        "database and returns 204 No Content on success, or 404 Not Found when the student does not exist."
    )]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateStudentCommand(id, dto), cancellationToken);
        if (!updated) return NotFound(new { message = $"Student with ID {id} not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType(typeof(AdminStudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a student's full academic detail")]
    [EndpointDescription(
        "Returns a comprehensive student profile for administrators and instructors, including " +
        "enrolled courses, enrollment statuses, progress, and final scores. Returns 404 Not Found " +
        "when the student ID does not exist."
    )]
    public async Task<IActionResult> GetStudentDetail(int id, CancellationToken cancellationToken)
    {
        var detail = await _mediator.Send(new GetStudentDetailQuery(id), cancellationToken);
        if (detail == null) return NotFound(new { message = $"Student with ID {id} not found." });

        return Ok(detail);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Suspend a student account")]
    [EndpointDescription(
        "Suspends the specified student account by setting its status to Suspended and, when a " +
        "linked user account exists, locking the user out indefinitely. The student can no longer " +
        "sign in until the account is reactivated. Returns 404 Not Found when the student does not exist."
    )]
    public async Task<IActionResult> SuspendStudent(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        student.Status = "Suspended";

        if (!string.IsNullOrEmpty(student.UserId))
        {
            var user = await _userManager.FindByIdAsync(student.UserId);
            if (user != null)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Student account suspended successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/probation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Place a student on academic probation")]
    [EndpointDescription(
        "Places the specified student on academic probation by setting the account status to " +
        "Probation. When a linked user account exists, any indefinite lockout is removed so the " +
        "student can sign in. Returns 404 Not Found when the student does not exist."
    )]
    public async Task<IActionResult> PlaceOnProbation(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        student.Status = "Probation";

        if (!string.IsNullOrEmpty(student.UserId))
        {
            var user = await _userManager.FindByIdAsync(student.UserId);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Student placed on probation successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Reactivate a student account")]
    [EndpointDescription(
        "Reactivates the specified student account by setting the account status back to Active " +
        "and removing any lockout on the linked user account. Returns 404 Not Found when the " +
        "student does not exist."
    )]
    public async Task<IActionResult> ActivateStudent(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        student.Status = "Active";

        if (!string.IsNullOrEmpty(student.UserId))
        {
            var user = await _userManager.FindByIdAsync(student.UserId);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Student account reactivated successfully." });
    }
}
