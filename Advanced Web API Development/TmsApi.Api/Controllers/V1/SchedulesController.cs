using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Extensions;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Schedules")]
public class SchedulesController(
    IScheduleService _scheduleService,
    TmsDbContext _context,
    IHubContext<TmsHub, ITmsClient> _hubContext) : ControllerBase
{

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ScheduleDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List schedules, filtered by audience")]
    [EndpointDescription(
        "Returns schedules optionally filtered by category, month, and year. Administrators see all " +
        "schedules, while other authenticated callers only receive the schedules relevant to their " +
        "linked student or instructor profile."
    )]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] string? category,
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        int? studentId = null;
        int? instructorId = null;

        if (!isAdmin && User.Identity?.IsAuthenticated == true)
        {
            studentId = await User.ResolveStudentIdAsync(_context);
            instructorId = await User.ResolveInstructorIdAsync(_context);
        }

        var schedules = await _scheduleService.GetSchedulesAsync(category, month, year, studentId, instructorId, isAdmin, cancellationToken);
        return Ok(schedules);
    }

    [Authorize]
    [HttpGet("my-schedule")]
    [ProducesResponseType(typeof(StudentScheduleSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the authenticated student's schedule overview")]
    [EndpointDescription(
        "Returns the schedule overview for the currently authenticated student, including class " +
        "counts for the week and month, the next upcoming class, enrolled courses, and the filtered " +
        "schedule list. Returns 404 Not Found when the user is not linked to a student profile."
    )]
    public async Task<IActionResult> GetMySchedule(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int? courseId,
        CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue)
        {
            return NotFound(new { message = "Student profile not found for authenticated user." });
        }

        var result = await _scheduleService.GetStudentScheduleOverviewAsync(studentId.Value, year, month, courseId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("instructor-schedule")]
    [ProducesResponseType(typeof(IEnumerable<ScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the authenticated instructor's schedule")]
    [EndpointDescription(
        "Returns the teaching schedule for the currently authenticated instructor, optionally " +
        "filtered by month, year, or date. Returns 404 Not Found when the user is not linked to an " +
        "instructor profile."
    )]
    public async Task<IActionResult> GetInstructorSchedule(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        var instructorId = await User.ResolveInstructorIdAsync(_context);
        if (!instructorId.HasValue)
        {
            return NotFound(new { message = "Instructor profile not found for authenticated user." });
        }

        var result = await _scheduleService.GetInstructorSchedulesAsync(instructorId.Value, month, year, date, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a schedule by ID")]
    [EndpointDescription(
        "Returns a single schedule entry. Returns 404 Not Found when the schedule does not exist."
    )]
    public async Task<IActionResult> GetScheduleById(int id, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleService.GetScheduleByIdAsync(id, cancellationToken);
        if (schedule == null) return NotFound(new { message = $"Schedule with ID {id} not found." });

        return Ok(schedule);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a new schedule entry")]
    [EndpointDescription(
        "Creates a new schedule entry (meeting, workshop, class, or event) with the provided " +
        "audience targeting. Triggers a real-time schedule update notification to all connected " +
        "clients. Returns 201 Created with the new schedule."
    )]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto dto, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.CreateScheduleAsync(dto, cancellationToken);
        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "created", id = result.Id });
        return CreatedAtAction(nameof(GetScheduleById), new { version = "1.0", id = result.Id }, result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update a schedule entry")]
    [EndpointDescription(
        "Updates the details of the specified schedule entry and triggers a real-time schedule " +
        "update notification to all connected clients. Returns 204 No Content on success or 404 " +
        "Not Found when the schedule does not exist."
    )]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleDto dto, CancellationToken cancellationToken)
    {
        var updated = await _scheduleService.UpdateScheduleAsync(id, dto, cancellationToken);
        if (!updated) return NotFound(new { message = $"Schedule with ID {id} not found." });

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "updated", id });
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete a schedule entry")]
    [EndpointDescription(
        "Permanently deletes the specified schedule entry and triggers a real-time schedule update " +
        "notification to all connected clients. Administrator-only operation. Returns 204 No " +
        "Content on success or 404 Not Found when the schedule does not exist."
    )]
    public async Task<IActionResult> DeleteSchedule(int id, CancellationToken cancellationToken)
    {
        var deleted = await _scheduleService.DeleteScheduleAsync(id, cancellationToken);
        if (!deleted) return NotFound(new { message = $"Schedule with ID {id} not found." });

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "deleted", id });
        return NoContent();
    }
}
