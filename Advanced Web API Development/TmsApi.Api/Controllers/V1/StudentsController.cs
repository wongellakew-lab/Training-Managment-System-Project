using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var response = await _studentService.GetStudentsAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStudentById(int id, CancellationToken cancellationToken)
    {
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
