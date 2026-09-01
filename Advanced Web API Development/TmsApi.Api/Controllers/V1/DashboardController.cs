using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Identity;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<TmsUser> _userManager;

    public DashboardController(IDashboardService dashboardService, UserManager<TmsUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    [Authorize]
    [HttpGet("student")]
    public async Task<IActionResult> GetStudentDashboard([FromQuery] int? studentId, CancellationToken cancellationToken)
    {
        int resolvedId = studentId ?? 0;
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var claim = User.FindFirst("studentId")?.Value;
            if (int.TryParse(claim, out var parsed))
            {
                resolvedId = parsed;
            }
        }

        if (resolvedId == 0)
        {
            var user = await _userManager.GetUserAsync(User);
            resolvedId = user?.StudentId ?? 1;
        }

        var result = await _dashboardService.GetStudentDashboardAsync(resolvedId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("instructor")]
    public async Task<IActionResult> GetInstructorDashboard([FromQuery] int? instructorId, CancellationToken cancellationToken)
    {
        int resolvedId = instructorId ?? 0;
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            var claim = User.FindFirst("instructorId")?.Value;
            if (int.TryParse(claim, out var parsed))
            {
                resolvedId = parsed;
            }
        }

        if (resolvedId == 0)
        {
            var user = await _userManager.GetUserAsync(User);
            resolvedId = user?.InstructorId ?? 1;
        }

        var result = await _dashboardService.GetInstructorDashboardAsync(resolvedId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetAdminDashboardAsync(cancellationToken);
        return Ok(result);
    }
}
