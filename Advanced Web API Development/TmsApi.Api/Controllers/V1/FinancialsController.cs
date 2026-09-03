using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.FinancialDtos;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class FinancialsController : ControllerBase
{
    private readonly TmsDbContext _context;

    public FinancialsController(TmsDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var result = new FinancialOverviewDto
        {
            TotalRevenue = 0m,
            TotalPaidOut = 0m,
            PendingReceivables = 0m,
            TotalTransactionsCount = enrollments.Count,
            RevenueGrowthPercentage = 0m,
            RecentTransactions = new List<FinancialTransactionDto>()
        };

        return Ok(result);
    }
}
