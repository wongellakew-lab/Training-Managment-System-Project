using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.ExternalServices;

public class CertificateService : ICertificateService
{
    private readonly TmsDbContext _context;
    private readonly IHubContext<TmsHub, ITmsClient> _hubContext;
    private readonly ILogger<CertificateService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private static readonly AsyncCircuitBreakerPolicy CircuitBreaker = Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));

    public CertificateService(
        TmsDbContext context,
        IHubContext<TmsHub, ITmsClient> hubContext,
        ILogger<CertificateService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Retry {RetryCount} for certificate issuance after {Delay}s", retryCount, timeSpan.TotalSeconds);
                });
    }

    public async Task<bool> IssueCertificateAsync(int studentId, int courseId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.FindAsync(new object[] { studentId }, cancellationToken);
        var course = await _context.Courses.FindAsync(new object[] { courseId }, cancellationToken);

        if (student == null || course == null) return false;

        var existingCert = await _context.Certificates.AnyAsync(
            c => c.StudentId == studentId && c.CourseId == courseId, cancellationToken);

        if (existingCert) return true;

        try
        {
            var certificateNumber = $"CERT-{DateTime.UtcNow.Year}-{studentId:D4}-{courseId:D4}";

            // Resilient simulation with Circuit Breaker and Retry
            await CircuitBreaker.ExecuteAsync(async () =>
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    // Simulated external verification endpoint call
                    await Task.Delay(100, cancellationToken);
                });
            });

            var cert = new Certificate
            {
                StudentId = studentId,
                CourseId = courseId,
                CertificateNumber = certificateNumber,
                IssueDate = DateTime.UtcNow,
                VerificationUrl = $"/api/v1/certificates/verify/{certificateNumber}"
            };

            await _context.Certificates.AddAsync(cert, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients.Group($"Student_{studentId}")
                .ReceiveNotification("Certificate Issued", $"Certificate {certificateNumber} issued for {course.Title}.");

            return true;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open. Certificate issuance temporarily suspended.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to issue certificate for student {StudentId}, course {CourseId}", studentId, courseId);
            return false;
        }
    }
}
