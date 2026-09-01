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

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetEnrollments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? studentId = null,
        [FromQuery] int? courseId = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _enrollmentService.GetEnrollmentsAsync(page, pageSize, studentId, courseId, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEnrollmentById(int id, CancellationToken cancellationToken)
    {
        var query = new GetEnrollmentByIdQuery(id);
        var enrollment = await _mediator.Send(query, cancellationToken);
        if (enrollment == null) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return Ok(enrollment);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> EnrollStudent([FromBody] CreateEnrollmentDto dto, CancellationToken cancellationToken)
    {
        var command = new EnrollStudentCommand(dto.StudentId, dto.CourseId);
        var result = await _mediator.Send(command, cancellationToken);

        EnrollmentsCounter.Add(1);

        return CreatedAtAction(nameof(GetEnrollmentById), new { version = "1.0", id = result.Id }, result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateEnrollmentStatus(int id, [FromBody] UpdateEnrollmentStatusDto dto, CancellationToken cancellationToken)
    {
        var updated = await _enrollmentService.UpdateEnrollmentStatusAsync(id, dto.Status, cancellationToken);
        if (!updated) return NotFound(new { message = $"Enrollment with ID {id} not found." });

        return NoContent();
    }
}
