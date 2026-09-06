using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Application.Attendance.Commands;
using TmsApi.Application.Attendance.Queries;
using TmsApi.Application.DTOs.AttendanceDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Attendance")]
public class AttendanceController(
    IMediator _mediator,
    TmsDbContext? _context = null)
    : ControllerBase
{

    private async Task<bool> CanManageCourseAttendanceAsync(int courseId)
    {
        if (User?.IsInRole("Admin") == true) return true;
        if (_context == null) return true;
        var instructorId = User != null ? await User.ResolveInstructorIdAsync(_context) : null;
        if (!instructorId.HasValue) return false;

        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        return course != null && course.InstructorId == instructorId.Value;
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("course/{courseId:int}")]
    [ProducesResponseType(typeof(CourseAttendanceSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course's attendance session")]
    [EndpointDescription(
        "Returns the recorded attendance snapshot for the specified course and date, listing every " +
        "enrolled student with their attendance status. Accessible to administrators and to the " +
        "instructor who owns the course. Returns 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> GetCourseAttendance(int courseId, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAttendanceAsync(courseId))
        {
            return Forbid();
        }

        var result = await _mediator.Send(new GetCourseAttendanceSessionQuery(courseId, date), cancellationToken);
        if (result == null) return NotFound(new { message = $"Course with ID {courseId} not found." });

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("mark")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Mark an individual student's attendance")]
    [EndpointDescription(
        "Records attendance for a single student in the specified course session. Returns 400 Bad " +
        "Request when the student is not enrolled in the course, and 403 Forbidden when the caller " +
        "is not the course owner or an administrator."
    )]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAttendanceAsync(request.CourseId))
        {
            return Forbid();
        }

        int? instructorId = _context != null && User != null ? await User.ResolveInstructorIdAsync(_context) : null;

        var success = await _mediator.Send(new MarkAttendanceCommand(
            request.StudentId,
            request.CourseId,
            request.ScheduleId,
            request.SessionDate,
            request.Status,
            request.Remarks,
            instructorId), cancellationToken);

        if (!success)
        {
            return BadRequest(new { message = "Student is not enrolled in this course." });
        }

        var statusName = Enum.IsDefined(typeof(Domain.Entities.AttendanceStatus), request.Status)
            ? ((Domain.Entities.AttendanceStatus)request.Status).ToString()
            : "Present";

        return Ok(new { message = "Attendance marked successfully.", status = statusName });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Save attendance for a class roll call")]
    [EndpointDescription(
        "Saves attendance for many students in the course in a single operation, returning the " +
        "number of students recorded. Returns 403 Forbidden when the caller is not the course " +
        "owner or an administrator."
    )]
    public async Task<IActionResult> SaveBulkAttendance([FromBody] BulkAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAttendanceAsync(request.CourseId))
        {
            return Forbid();
        }

        int? instructorId = _context != null && User != null ? await User.ResolveInstructorIdAsync(_context) : null;

        var dto = new BulkAttendanceRequestDto
        {
            CourseId = request.CourseId,
            ScheduleId = request.ScheduleId,
            SessionDate = request.SessionDate,
            Items = request.Items.Select(i => new BulkAttendanceItemDto
            {
                StudentId = i.StudentId,
                Status = i.Status,
                Remarks = i.Remarks
            }).ToList()
        };

        var savedCount = await _mediator.Send(new SaveBulkAttendanceCommand(dto, instructorId), cancellationToken);
        return Ok(new { message = $"Roll call saved successfully for {savedCount} students." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("stats/course/{courseId:int}")]
    [ProducesResponseType(typeof(CourseAttendanceStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get a course's attendance statistics")]
    [EndpointDescription(
        "Returns aggregate attendance statistics for the specified course, including session " +
        "count, present, late, and absent totals, and the overall attendance rate. Accessible to " +
        "the course owner and administrators."
    )]
    public async Task<IActionResult> GetCourseAttendanceStats(int courseId, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAttendanceAsync(courseId))
        {
            return Forbid();
        }

        var stats = await _mediator.Send(new GetCourseAttendanceStatsQuery(courseId), cancellationToken);
        return Ok(stats);
    }

}
