using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;

    public SchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] string? category,
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var schedules = await _scheduleService.GetSchedulesAsync(category, month, year, cancellationToken);
        return Ok(schedules);
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
        return CreatedAtAction(nameof(GetScheduleById), new { version = "1.0", id = result.Id }, result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleDto dto, CancellationToken cancellationToken)
    {
        var updated = await _scheduleService.UpdateScheduleAsync(id, dto, cancellationToken);
        if (!updated) return NotFound(new { message = $"Schedule with ID {id} not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchedule(int id, CancellationToken cancellationToken)
    {
        var deleted = await _scheduleService.DeleteScheduleAsync(id, cancellationToken);
        if (!deleted) return NotFound(new { message = $"Schedule with ID {id} not found." });

        return NoContent();
    }
}
