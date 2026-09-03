using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly TmsDbContext _context;

    public StudentsController(IStudentService studentService, TmsDbContext context)
    {
        _studentService = studentService;
        _context = context;
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var response = await _studentService.GetStudentsAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStudentStats(CancellationToken cancellationToken)
    {
        var totalStudents = await _context.Students.CountAsync(s => !s.IsDeleted, cancellationToken);
        var activeEnrolled = await _context.Enrollments
            .Where(e => e.Status == EnrollmentStatus.Active)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);
        var certificatesIssued = await _context.Certificates.CountAsync(cancellationToken);

        return Ok(new StudentStatsDto
        {
            TotalStudents = totalStudents,
            ActiveEnrolled = activeEnrolled,
            CertificatesIssued = certificatesIssued
        });
    }

    [Authorize]
    [HttpGet("{id:int}")]
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

        var student = await _studentService.GetStudentByIdAsync(id, cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        return Ok(student);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto, CancellationToken cancellationToken)
    {
        var student = await _studentService.CreateStudentAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetStudentById), new { version = "1.0", id = student.Id }, student);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto, CancellationToken cancellationToken)
    {
        var updated = await _studentService.UpdateStudentAsync(id, dto, cancellationToken);
        if (!updated) return NotFound(new { message = $"Student with ID {id} not found." });

        return NoContent();
    }
}
