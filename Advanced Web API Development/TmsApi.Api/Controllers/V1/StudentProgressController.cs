using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Extensions;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Students.Queries;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/student/progress")]
[Authorize(Roles = "Student,Admin")]
[Tags("Students")]
public class StudentProgressController(
    IMediator _mediator,
    TmsDbContext _context)
    : ControllerBase
{
    [HttpGet("{courseId:int}")]
    [ProducesResponseType(typeof(StudentCourseProgressDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the authenticated student's progress in a course")]
    [EndpointDescription(
        "Returns detailed progress for the currently authenticated student in the specified course, " +
        "including course summary, module and lesson completion, attendance, assignments, and final " +
        "grade. Returns 404 Not Found when the student is not enrolled in the course or the course " +
        "does not exist."
    )]
    public async Task<IActionResult> GetCourseProgressDetails(int courseId, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        var result = await _mediator.Send(new GetCourseProgressDetailQuery(studentId.Value, courseId), cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "You are not enrolled in this course or the course does not exist." });
        }

        return Ok(result);
    }
}
