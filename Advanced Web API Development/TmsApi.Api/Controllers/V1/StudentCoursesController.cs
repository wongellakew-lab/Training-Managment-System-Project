using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Extensions;
using TmsApi.Application.Courses.Queries;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Students.Commands;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/student/my-courses")]
[Authorize(Roles = "Student,Admin")]
[Tags("Students")]
public class StudentCoursesController(
    IMediator _mediator,
    TmsDbContext _context)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<StudentEnrolledCourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get the authenticated student's enrolled courses")]
    [EndpointDescription(
        "Returns all courses the currently authenticated student is enrolled in, including schedule, " +
        "instructor, and progress information. Only the courses of the authenticated student are " +
        "returned based on the linked student profile."
    )]
    public async Task<IActionResult> GetMyCourses(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        var courses = await _mediator.Send(new GetStudentEnrolledCoursesQuery(studentId.Value), cancellationToken);
        return Ok(courses);
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(StudentCoursesStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get the authenticated student's course statistics")]
    [EndpointDescription(
        "Returns aggregate statistics for the currently authenticated student's enrollment, such as " +
        "total enrolled courses, active, completed, and upcoming counts, total credits, and overall " +
        "progress percentage."
    )]
    public async Task<IActionResult> GetMyCoursesStatistics(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        var statistics = await _mediator.Send(new GetStudentCoursesStatisticsQuery(studentId.Value), cancellationToken);
        return Ok(statistics);
    }

    [HttpPost("{courseId:int}/withdraw")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Withdraw the authenticated student from a course")]
    [EndpointDescription(
        "Withdraws the currently authenticated student from the specified course. Returns 404 Not " +
        "Found when the student is not enrolled in the course or the course does not exist, and 400 " +
        "Bad Request when the enrollment cannot be withdrawn in its current state."
    )]
    public async Task<IActionResult> WithdrawFromCourse(int courseId, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        try
        {
            var success = await _mediator.Send(new WithdrawFromCourseCommand(studentId.Value, courseId), cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "You are not enrolled in this course or course not found." });
            }

            return Ok(new
            {
                message = "Successfully withdrawn from course.",
                courseId = courseId,
                status = "Withdrawn"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
