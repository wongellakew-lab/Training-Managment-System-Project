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
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<TmsUser> _userManager;
    private readonly TmsDbContext _context;

    public DashboardController(
        IDashboardService dashboardService,
        UserManager<TmsUser> userManager,
        TmsDbContext context)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
        _context = context;
    }

    [Authorize]
    [HttpGet("student")]
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
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetAdminDashboardAsync(cancellationToken);
        return Ok(result);
    }

    public record ReplyQuestionRequest(string Answer);

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("instructor/questions/{id:int}/reply")]
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
