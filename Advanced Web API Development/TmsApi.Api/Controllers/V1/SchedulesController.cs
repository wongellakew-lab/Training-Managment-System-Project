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
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly TmsDbContext _context;
    private readonly IHubContext<TmsHub, ITmsClient> _hubContext;

    public SchedulesController(
        IScheduleService scheduleService,
        TmsDbContext context,
        IHubContext<TmsHub, ITmsClient> hubContext)
    {
        _scheduleService = scheduleService;
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
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
    public async Task<IActionResult> GetScheduleById(int id, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleService.GetScheduleByIdAsync(id, cancellationToken);
        if (schedule == null) return NotFound(new { message = $"Schedule with ID {id} not found." });

        return Ok(schedule);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto dto, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.CreateScheduleAsync(dto, cancellationToken);
        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "created", id = result.Id });
        return CreatedAtAction(nameof(GetScheduleById), new { version = "1.0", id = result.Id }, result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleDto dto, CancellationToken cancellationToken)
    {
        var updated = await _scheduleService.UpdateScheduleAsync(id, dto, cancellationToken);
        if (!updated) return NotFound(new { message = $"Schedule with ID {id} not found." });

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "updated", id });
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchedule(int id, CancellationToken cancellationToken)
    {
        var deleted = await _scheduleService.DeleteScheduleAsync(id, cancellationToken);
        if (!deleted) return NotFound(new { message = $"Schedule with ID {id} not found." });

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "deleted", id });
        return NoContent();
    }
}
