using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICachedCourseService _courseService;

    public CoursesController(ICachedCourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Fields))
        {
            var shapedResponse = await _courseService.GetShapedCoursesAsync(request, cancellationToken);
            return Ok(shapedResponse);
        }

        var response = await _courseService.GetCoursesAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCourseByIdAsync(id, cancellationToken);
        if (course == null) return NotFound(new { message = $"Course with ID {id} not found." });

        return Ok(course);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto, CancellationToken cancellationToken)
    {
        var course = await _courseService.CreateCourseAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetCourseById), new { version = "2.0", id = course.Id }, course);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto, CancellationToken cancellationToken)
    {
        var updated = await _courseService.UpdateCourseAsync(id, dto, cancellationToken);
        if (!updated) return NotFound(new { message = $"Course with ID {id} not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken cancellationToken)
    {
        var deleted = await _courseService.DeleteCourseAsync(id, cancellationToken);
        if (!deleted) return NotFound(new { message = $"Course with ID {id} not found." });

        return NoContent();
    }
}
