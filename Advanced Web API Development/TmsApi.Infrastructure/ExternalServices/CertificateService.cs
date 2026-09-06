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

public class CertificateService(
    TmsDbContext _context,
    IHubContext<TmsHub, ITmsClient> _hubContext,
    ILogger<CertificateService> _logger) : ICertificateService
{
    private static readonly AsyncCircuitBreakerPolicy CircuitBreaker = Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning(exception, "Retry {RetryCount} for certificate issuance after {Delay}s", retryCount, timeSpan.TotalSeconds);
            });

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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentId == studentId, cancellationToken);
            var notifUserId = user?.Id ?? student.UserId ?? studentId.ToString();

            var notification = new Notification
            {
                UserId = notifUserId,
                Title = "Certificate Issued",
                Message = $"Congratulations! You have completed {course.Title} and earned certificate {certificateNumber}.",
                Type = "Certificate",
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = cert.Id,
                RelatedEntityType = "Certificate"
            };
            await _context.Notifications.AddAsync(notification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients.Group($"Student_{studentId}")
                .ReceiveNotification("Certificate Issued", $"Certificate {certificateNumber} issued for {course.Title}.");

            if (user != null)
            {
                await _hubContext.Clients.Group($"User_{user.Id}")
                    .ReceiveNotification("Certificate Issued", $"Certificate {certificateNumber} issued for {course.Title}.");
            }

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

    public async Task<bool> CheckAndIssueCertificateIfEligibleAsync(int studentId, int courseId, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
        if (course == null || course.IsDeleted) return false;

        // 1. Check if course gives Completion Certificate
        if (!CourseOffersCertificate(course.FeaturesJson))
        {
            return false;
        }

        // 2. Check enrollment
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == studentId && !e.IsDeleted, cancellationToken);
        if (enrollment == null || enrollment.Status == EnrollmentStatus.Dropped || enrollment.Status == EnrollmentStatus.Rejected)
        {
            return false;
        }

        // 3. Check published assignments for this course
        var publishedAssignments = await _context.Assignments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && !a.IsDeleted && a.IsPublished)
            .ToListAsync(cancellationToken);

        if (!publishedAssignments.Any())
        {
            return false;
        }

        var publishedAssignmentIds = publishedAssignments.Select(a => a.Id).ToHashSet();

        // 4. Ensure all published assessments are evaluated
        var studentAssessments = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId &&
                        a.StudentId == studentId &&
                        a.AssignmentId.HasValue &&
                        publishedAssignmentIds.Contains(a.AssignmentId.Value))
            .ToListAsync(cancellationToken);

        var gradedAssessments = studentAssessments
            .Where(a => a.Status == "Graded" && a.ScoredPoints.HasValue)
            .ToList();

        if (gradedAssessments.Count < publishedAssignments.Count)
        {
            return false;
        }

        // 5. Check passing mark (>= 60%)
        decimal totalScored = gradedAssessments.Sum(a => a.ScoredPoints!.Value);
        decimal totalMax = gradedAssessments.Sum(a => a.MaxScore);
        decimal avgPct = totalMax > 0 ? Math.Round((totalScored / totalMax) * 100m, 1) : 0m;

        bool isPassing = avgPct >= 60m || (enrollment.Grade.HasValue && enrollment.Grade.Value >= 60m);
        if (!isPassing)
        {
            return false;
        }

        // 6. Update enrollment status to Completed if not already
        if (enrollment.Status != EnrollmentStatus.Completed)
        {
            enrollment.Status = EnrollmentStatus.Completed;
            if (!enrollment.Grade.HasValue || enrollment.Grade.Value < avgPct)
            {
                enrollment.Grade = avgPct;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 7. Issue the official certificate
        return await IssueCertificateAsync(studentId, courseId, cancellationToken);
    }

    public static bool CourseOffersCertificate(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson) || featuresJson.Trim() == "[]") return false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(featuresJson);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("certificate", out var certProp))
                {
                    if (certProp.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                    if (certProp.ValueKind == System.Text.Json.JsonValueKind.False) return false;
                }
                if (doc.RootElement.TryGetProperty("hasCertificate", out var hasCertProp))
                {
                    if (hasCertProp.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                }
                if (doc.RootElement.TryGetProperty("completionCertificate", out var compCertProp))
                {
                    if (compCertProp.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                }
            }
            else if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var elem in doc.RootElement.EnumerateArray())
                {
                    if (elem.ValueKind == System.Text.Json.JsonValueKind.String &&
                        elem.GetString()?.IndexOf("certificate", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            return featuresJson.IndexOf("certificate", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   !featuresJson.Contains("\"certificate\":false", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
