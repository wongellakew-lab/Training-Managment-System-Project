namespace TmsApi.Domain.Entities;

public class Certificate
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public string VerificationUrl { get; set; } = string.Empty;
}
