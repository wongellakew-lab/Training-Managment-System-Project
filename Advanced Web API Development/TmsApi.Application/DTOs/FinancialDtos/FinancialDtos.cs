namespace TmsApi.Application.DTOs.FinancialDtos;

public class FinancialOverviewDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaidOut { get; set; }
    public decimal PendingReceivables { get; set; }
    public int TotalTransactionsCount { get; set; }
    public decimal RevenueGrowthPercentage { get; set; }
    public List<FinancialTransactionDto> RecentTransactions { get; set; } = new();
}

public class FinancialTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
