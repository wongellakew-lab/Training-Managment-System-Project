namespace TmsApi.Application.Interfaces;

public interface ICertificateService
{
    Task<bool> IssueCertificateAsync(int studentId, int courseId, CancellationToken cancellationToken = default);
    Task<bool> CheckAndIssueCertificateIfEligibleAsync(int studentId, int courseId, CancellationToken cancellationToken = default);
}
