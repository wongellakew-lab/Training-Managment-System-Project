using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
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

        // If user is authenticated, check enrollment status
        var claim = User.FindFirst("studentId")?.Value;
        if (int.TryParse(claim, out var studentId))
        {
            var enrollment = course.Enrollments?.FirstOrDefault(e => e.StudentId == studentId && e.Status != Domain.Entities.EnrollmentStatus.Dropped && e.Status != Domain.Entities.EnrollmentStatus.Rejected);
            if (enrollment != null)
            {
                course.IsStudentEnrolled = true;
                course.StudentEnrollmentStatus = enrollment.Status.ToString();
                course.StudentProgress = enrollment.Grade.HasValue ? enrollment.Grade.Value : null;
            }
        }

        return Ok(course);
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var response = await _courseService.GetCoursesAsync(request, cancellationToken);
        var claim = User.FindFirst("studentId")?.Value;
        if (int.TryParse(claim, out var studentId) && response.Items != null)
        {
            foreach (var item in response.Items)
            {
                // Check if current student is enrolled in this course
                var courseDetail = await _courseService.GetCourseByIdAsync(item.Id, cancellationToken);
                var enrollment = courseDetail?.Enrollments?.FirstOrDefault(e => e.StudentId == studentId && e.Status != Domain.Entities.EnrollmentStatus.Dropped && e.Status != Domain.Entities.EnrollmentStatus.Rejected);
                if (enrollment != null)
                {
                    item.IsStudentEnrolled = true;
                    item.StudentEnrollmentStatus = enrollment.Status.ToString();
                }
            }
        }
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto, CancellationToken cancellationToken)
    {
        var course = await _courseService.CreateCourseAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetCourseById), new { version = "1.0", id = course.Id }, course);
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
