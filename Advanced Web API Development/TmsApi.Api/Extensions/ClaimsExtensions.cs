using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Extensions;

public static class ClaimsExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? principal.FindFirst("sub")?.Value;
    }

    public static int? GetStudentId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("studentId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public static int? GetInstructorId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("instructorId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public static async Task<int?> ResolveStudentIdAsync(this ClaimsPrincipal principal, TmsDbContext context, UserManager<TmsUser>? userManager = null)
    {
        var id = principal.GetStudentId();
        if (id.HasValue && id.Value > 0) return id.Value;

        var userId = principal.GetUserId();
        if (string.IsNullOrEmpty(userId)) return null;

        var student = await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student != null) return student.Id;

        if (userManager != null)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user?.StudentId.HasValue == true) return user.StudentId.Value;
        }
        else
        {
            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.StudentId.HasValue == true) return user.StudentId.Value;
        }

        return null;
    }

    public static async Task<int?> ResolveInstructorIdAsync(this ClaimsPrincipal principal, TmsDbContext context, UserManager<TmsUser>? userManager = null)
    {
        var id = principal.GetInstructorId();
        if (id.HasValue && id.Value > 0) return id.Value;

        var userId = principal.GetUserId();
        if (string.IsNullOrEmpty(userId)) return null;

        var instructor = await context.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserId == userId);

        if (instructor != null) return instructor.Id;

        if (userManager != null)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user?.InstructorId.HasValue == true) return user.InstructorId.Value;
        }
        else
        {
            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.InstructorId.HasValue == true) return user.InstructorId.Value;
        }

        return null;
    }
}
