using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Authorization;

public class MinimumGpaRequirement : IAuthorizationRequirement
{
    public decimal MinimumGpa { get; }

    public MinimumGpaRequirement(decimal minimumGpa)
    {
        MinimumGpa = minimumGpa;
    }
}

public class MinimumGpaHandler : AuthorizationHandler<MinimumGpaRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public MinimumGpaHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumGpaRequirement requirement)
    {
        var studentIdClaim = context.User.FindFirst("studentId");
        if (studentIdClaim == null || !int.TryParse(studentIdClaim.Value, out var studentId))
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

        var student = await dbContext.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId);
        if (student != null && student.Gpa >= requirement.MinimumGpa)
        {
            context.Succeed(requirement);
        }
    }
}
