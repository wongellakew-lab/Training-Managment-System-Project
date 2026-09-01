using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using static TmsApi.Api.Diagnostics.Diagnostics;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IMediator mediator, IEnrollmentService enrollmentService)
    {
        _mediator = mediator;
        _enrollmentService = enrollmentService;
    }

    private int? GetCurrentStudentId()
    {
        var claim = User.FindFirst("studentId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
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
            var currentStudentId = GetCurrentStudentId();
            if (currentStudentId.HasValue)
            {
                studentId = currentStudentId.Value;
            }
        }

        var response = await _enrollmentService.GetEnrollmentsAsync(page, pageSize, studentId, courseId, search, status, category, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("breakdown")]
    public async Task<IActionResult> GetPaymentBreakdown(CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.GetPaymentBreakdownAsync(cancellationToken);
        return Ok(result);
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
            var currentStudentId = GetCurrentStudentId();
            if (currentStudentId.HasValue && enrollment.StudentId != currentStudentId.Value)
            {
                return Forbid();
            }
        }

        return Ok(enrollment);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> EnrollStudent([FromBody] CreateEnrollmentDto dto, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin"))
        {
            var currentStudentId = GetCurrentStudentId();
            if (currentStudentId.HasValue)
            {
                dto.StudentId = currentStudentId.Value;
            }
        }

        var created = await _enrollmentService.CreateEnrollmentAsync(dto, cancellationToken);
        EnrollmentsCounter.Add(1);

        return CreatedAtAction(nameof(GetEnrollmentById), new { version = "1.0", id = created.Id }, created);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateEnrollmentStatus(int id, [FromBody] UpdateEnrollmentStatusDto dto, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, dto.Status, dto.PaymentStatus, cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, EnrollmentStatus.Active, "Paid", cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return Ok(new { message = "Enrollment approved successfully." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> RejectEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, EnrollmentStatus.Rejected, null, cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return Ok(new { message = "Enrollment rejected." });
    }

    [Authorize]
    [HttpPut("{id:int}/drop")]
    public async Task<IActionResult> DropEnrollment(int id, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, EnrollmentStatus.Dropped, null, cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return Ok(new { message = "Course enrollment dropped." });
    }
}
