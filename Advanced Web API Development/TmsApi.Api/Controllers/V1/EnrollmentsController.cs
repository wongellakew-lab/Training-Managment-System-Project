using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using static TmsApi.Api.Diagnostics.Diagnostics;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEnrollmentService _enrollmentService;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<TmsHub, ITmsClient> _hubContext;
    private readonly TmsDbContext _context;

    public EnrollmentsController(
        IMediator mediator,
        IEnrollmentService enrollmentService,
        INotificationService notificationService,
        IHubContext<TmsHub, ITmsClient> hubContext,
        TmsDbContext context)
    {
        _mediator = mediator;
        _enrollmentService = enrollmentService;
        _notificationService = notificationService;
        _hubContext = hubContext;
        _context = context;
    }

    private async Task<int?> GetCurrentStudentIdAsync()
    {
        return await User.ResolveStudentIdAsync(_context);
    }

    [Authorize]
    [HttpGet]
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
        // If caller is student, constrain to their own studentId
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var currentStudentId = await GetCurrentStudentIdAsync();
            if (currentStudentId.HasValue)
                studentId = currentStudentId.Value;
        }

        var response = await _enrollmentService.GetEnrollmentsAsync(page, pageSize, studentId, courseId, search, status, category, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEnrollmentById(int id, CancellationToken cancellationToken)
    {
        var query = new GetEnrollmentByIdQuery(id);
        var enrollment = await _mediator.Send(query, cancellationToken);
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
            var created = await _enrollmentService.CreateEnrollmentAsync(dto, cancellationToken);
            EnrollmentsCounter.Add(1);

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.StudentId, cancellationToken);
            var notifUserId = student?.UserId ?? dto.StudentId.ToString();

            // Create in-app notification in DB
            await _notificationService.CreateNotificationAsync(
                notifUserId,
                "Enrollment Submitted",
                $"Your registration for '{created.CourseTitle}' ({created.CourseCode}) has been registered.",
                "Enrollment",
                created.Id,
                "Enrollment",
                cancellationToken);

            // Emit real-time SignalR event
            await _hubContext.Clients.Group($"Student_{dto.StudentId}").ReceiveEnrollmentNotification(new
            {
                studentId = dto.StudentId,
                courseId = dto.CourseId,
                status = created.Status
            });

            if (!string.IsNullOrEmpty(student?.UserId))
            {
                await _hubContext.Clients.Group($"User_{student.UserId}").ReceiveNotification(
                    "Enrollment Submitted",
                    $"Your registration for '{created.CourseTitle}' has been registered.");
            }

            // Real-time calendar schedule update
            await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "enrolled", studentId = dto.StudentId, courseId = dto.CourseId });

            // Real-time capacity update
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.CourseId, cancellationToken);
            if (course != null)
            {
                var enrolledCount = await _context.Enrollments.CountAsync(e => e.CourseId == dto.CourseId && (e.Status == Domain.Entities.EnrollmentStatus.Active || e.Status == Domain.Entities.EnrollmentStatus.Pending) && !e.IsDeleted, cancellationToken);
                await _hubContext.Clients.All.ReceiveCapacityAlert(new
                {
                    courseId = course.Id,
                    enrolledCount = enrolledCount,
                    maxCapacity = course.MaxCapacity
                });
            }

            return CreatedAtAction(nameof(GetEnrollmentById), new { version = "1.0", id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateEnrollmentStatus(int id, [FromBody] UpdateEnrollmentStatusDto dto, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, dto.Status, cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "statusChanged", id });
        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, EnrollmentStatus.Active, cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        await NotifyStudentOnStatusChange(id, "Enrollment Approved",
            "Your course enrollment has been approved! You are now an active student.", "Enrollment", cancellationToken);

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "approved", id });
        return Ok(new { message = "Enrollment approved successfully." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, EnrollmentStatus.Rejected, cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        await NotifyStudentOnStatusChange(id, "Enrollment Rejected",
            "Your enrollment request was not approved. Please contact your instructor for details.", "Enrollment", cancellationToken);

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "rejected", id });
        return Ok(new { message = "Enrollment rejected." });
    }

    [Authorize]
    [HttpPut("{id:int}/drop")]
    public async Task<IActionResult> DropEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, EnrollmentStatus.Dropped, cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "dropped", id });
        return Ok(new { message = "Course enrollment dropped." });
    }

    /// <summary>
    /// Helper: find the student's TmsUser, create a DB notification, and push a SignalR event.
    /// </summary>
    private async Task NotifyStudentOnStatusChange(int enrollmentId, string title, string message, string type, CancellationToken cancellationToken)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);
        if (enrollment == null) return;

        var tmsUser = await _context.Users
            .FirstOrDefaultAsync(u => u.StudentId == enrollment.StudentId, cancellationToken);

        if (tmsUser != null)
        {
            // Persist to DB
            await _notificationService.CreateNotificationAsync(
                tmsUser.Id,
                title,
                $"{message} Course: {enrollment.Course?.Title ?? ""}",
                type,
                enrollmentId,
                "Enrollment",
                cancellationToken);

            // Push via SignalR to the student's user group
            await _hubContext.Clients
                .Group($"User_{tmsUser.Id}")
                .ReceiveNotification(title, $"{message} Course: {enrollment.Course?.Title ?? ""}");
        }
    }
}
