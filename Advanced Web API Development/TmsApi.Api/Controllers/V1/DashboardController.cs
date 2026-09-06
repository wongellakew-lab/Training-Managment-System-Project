using TmsApi.Application.DTOs.DashboardDtos;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Extensions;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Dashboard")]
public class DashboardController(
    IDashboardService _dashboardService,
    UserManager<TmsUser> _userManager,
    TmsDbContext _context)
    : ControllerBase
{

    [Authorize]
    [HttpGet("student")]
    [ProducesResponseType(typeof(StudentDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the student dashboard")]
    [EndpointDescription(
        "Returns the dashboard summary for a student, including cumulative GPA, credits, " +
        "enrollment counts, upcoming classes, pending assignments, recent grades, and course " +
        "progress. A student caller is always scoped to their own profile regardless of any " +
        "supplied student ID; administrators and instructors may target another student. Returns " +
        "404 Not Found when the student profile cannot be resolved."
    )]
    public async Task<IActionResult> GetStudentDashboard([FromQuery] int? studentId, CancellationToken cancellationToken)
    {
        int resolvedId;
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var currentStudentId = await User.ResolveStudentIdAsync(_context, _userManager);
            if (!currentStudentId.HasValue)
            {
                return NotFound(new { message = "Student profile not found for authenticated user." });
            }
            resolvedId = currentStudentId.Value;
        }
        else
        {
            // Admin or Instructor viewing specific student or self
            resolvedId = studentId ?? (await User.ResolveStudentIdAsync(_context, _userManager)) ?? 1;
        }

        var result = await _dashboardService.GetStudentDashboardAsync(resolvedId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("instructor")]
    [ProducesResponseType(typeof(InstructorDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the instructor dashboard")]
    [EndpointDescription(
        "Returns the dashboard summary for an instructor, including assigned courses, student " +
        "counts, pending grading, today's classes, upcoming sessions, and recent student questions. " +
        "An instructor caller is always scoped to their own profile regardless of any supplied " +
        "instructor ID; administrators may target another instructor. Returns 404 Not Found when " +
        "the instructor profile cannot be resolved."
    )]
    public async Task<IActionResult> GetInstructorDashboard([FromQuery] int? instructorId, [FromQuery] DateTime? clientTime, CancellationToken cancellationToken)
    {
        int resolvedId;
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            var currentInstructorId = await User.ResolveInstructorIdAsync(_context, _userManager);
            if (!currentInstructorId.HasValue)
            {
                return NotFound(new { message = "Instructor profile not found for authenticated user." });
            }
            resolvedId = currentInstructorId.Value;
        }
        else
        {
            resolvedId = instructorId ?? (await User.ResolveInstructorIdAsync(_context, _userManager)) ?? 1;
        }

        var result = await _dashboardService.GetInstructorDashboardAsync(resolvedId, clientTime, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get the admin dashboard")]
    [EndpointDescription(
        "Returns the administrative dashboard summary, including student, instructor, course, and " +
        "enrollment counts, enrollment trends, top course distribution, recent schedules, and the " +
        "recent system activity feed. Administrator-only endpoint."
    )]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetAdminDashboardAsync(cancellationToken);
        return Ok(result);
    }


    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("instructor/questions/{id:int}/reply")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Reply to a student question")]
    [EndpointDescription(
        "Saves an answer to the specified student question, marking it as answered with the " +
        "current time and the answering instructor. Returns 404 Not Found when the question does " +
        "not exist."
    )]
    public async Task<IActionResult> ReplyToStudentQuestion(int id, [FromBody] ReplyQuestionRequest request, CancellationToken cancellationToken)
    {
        var question = await _context.StudentQuestions.FindAsync(new object[] { id }, cancellationToken);
        if (question == null) return NotFound(new { message = "Question not found." });

        var instructorId = await User.ResolveInstructorIdAsync(_context, _userManager);
        question.Answer = request.Answer;
        question.IsAnswered = true;
        question.AnsweredAt = DateTime.UtcNow;
        question.AnsweredByInstructorId = instructorId;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Answer saved successfully." });
    }
}
