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
[Tags("Courses")]
public class CoursesController(
    ICachedCourseService _courseService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CourseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List courses (cached) with search, filters, and field shaping")]
    [EndpointDescription(
        "Returns a paged list of courses using a cached backing store for improved performance. " +
        "When the Fields query parameter is supplied, the response is shaped to return only the " +
        "requested fields. This is the V2 cached alternative to the V1 Courses endpoint."
    )]
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
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course by ID (cached)")]
    [EndpointDescription(
        "Returns the complete detail of a single course from the cached backing store. Returns " +
        "404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCourseByIdAsync(id, cancellationToken);
        if (course == null) return NotFound(new { message = $"Course with ID {id} not found." });

        return Ok(course);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a new course (cached)")]
    [EndpointDescription(
        "Creates a new course using the cached backing store and returns 201 Created with the new " +
        "course. Administrator or instructor access only."
    )]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto, CancellationToken cancellationToken)
    {
        var course = await _courseService.CreateCourseAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetCourseById), new { version = "2.0", id = course.Id }, course);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update an existing course (cached)")]
    [EndpointDescription(
        "Updates the details of the specified course using the cached backing store and returns 204 " +
        "No Content on success, or 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto, CancellationToken cancellationToken)
    {
        var updated = await _courseService.UpdateCourseAsync(id, dto, cancellationToken);
        if (!updated) return NotFound(new { message = $"Course with ID {id} not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete a course (cached)")]
    [EndpointDescription(
        "Permanently deletes the specified course via the cached backing store. Returns 204 No " +
        "Content on success and 404 Not Found when the course does not exist."
    )]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken cancellationToken)
    {
        var deleted = await _courseService.DeleteCourseAsync(id, cancellationToken);
        if (!deleted) return NotFound(new { message = $"Course with ID {id} not found." });

        return NoContent();
    }
}
