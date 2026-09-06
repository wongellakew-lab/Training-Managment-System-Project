using Asp.Versioning;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Instructors.Commands;
using TmsApi.Application.Instructors.Queries;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Instructors")]
public class InstructorsController(
    IMediator _mediator)
    : ControllerBase
{

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<InstructorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("List instructors with paging, search, and filters")]
    [EndpointDescription(
        "Returns a paged list of instructors for any authenticated user. Supports paging, full-text " +
        "search, and filtering by department through the PagedRequest query parameters."
    )]
    public async Task<IActionResult> GetInstructors(
        [FromQuery] PagedRequest request,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetInstructorsQuery(request, department), cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("stats")]
    [ProducesResponseType(typeof(InstructorStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Get aggregate instructor statistics")]
    [EndpointDescription(
        "Returns high-level statistics about the instructor body, such as the total number of " +
        "instructors, active mentors, and average rating. Available to any authenticated user."
    )]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _mediator.Send(new GetInstructorStatsQuery(), cancellationToken);
        return Ok(stats);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstructorDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an instructor by ID")]
    [EndpointDescription(
        "Returns the profile, statistics, and courses of a single instructor. Returns 404 Not Found " +
        "when the instructor ID does not exist."
    )]
    public async Task<IActionResult> GetInstructorById(int id, CancellationToken cancellationToken)
    {
        var instructor = await _mediator.Send(new GetInstructorByIdQuery(id), cancellationToken);
        if (instructor == null)
            return NotFound(new { message = $"Instructor with ID {id} not found." });

        return Ok(instructor);
    }

    [Authorize]
    [HttpGet("{id:int}/schedules")]
    [ProducesResponseType(typeof(IEnumerable<ScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an instructor's teaching schedules")]
    [EndpointDescription(
        "Returns the teaching schedules for the specified instructor, optionally filtered by month, " +
        "year, or a specific date. Returns 404 Not Found when the instructor ID does not exist."
    )]
    public async Task<IActionResult> GetInstructorSchedules(
        int id,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        var schedules = await _mediator.Send(new GetInstructorSchedulesQuery(id, month, year, date), cancellationToken);
        if (schedules == null)
            return NotFound(new { message = $"Instructor with ID {id} not found." });

        return Ok(schedules);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(InstructorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a new instructor record")]
    [EndpointDescription(
        "Creates a new instructor record for an administrator, including an employee number, name, " +
        "email, and department. Returns 201 Created with the new instructor, or 400 Bad Request when " +
        "the data is invalid or an instructor already exists for the email."
    )]
    public async Task<IActionResult> CreateInstructor([FromBody] CreateInstructorDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _mediator.Send(new CreateInstructorCommand(dto), cancellationToken);
            return CreatedAtAction(nameof(GetInstructorById), new { version = "1.0", id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Suspend an instructor and reassign their courses")]
    [EndpointDescription(
        "Suspends the specified instructor and reassigns their active courses to replacement " +
        "instructors listed in the request. Returns 400 Bad Request when a suspension cannot be " +
        "performed (for example when no valid replacement is provided) and 404 Not Found when the " +
        "instructor does not exist."
    )]
    public async Task<IActionResult> SuspendInstructor(
        int id,
        [FromBody] SuspendInstructorRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _mediator.Send(new SuspendInstructorCommand(id, request), cancellationToken);
            if (!success) return NotFound(new { message = $"Instructor with ID {id} not found." });
            return Ok(new { message = "Instructor suspended successfully and courses reassigned." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to suspend instructor.", details = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Reactivate a suspended instructor")]
    [EndpointDescription(
        "Reactivates the specified instructor and restores eligible courses that were previously " +
        "reassigned during suspension. Returns 400 Bad Request when the instructor cannot be " +
        "reactivated and 404 Not Found when the instructor does not exist."
    )]
    public async Task<IActionResult> ActivateInstructor(int id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _mediator.Send(new ActivateInstructorCommand(id), cancellationToken);
            if (!success) return NotFound(new { message = $"Instructor with ID {id} not found." });
            return Ok(new { message = "Instructor reactivated successfully and eligible courses restored." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to reactivate instructor.", details = ex.Message });
        }
    }

}
