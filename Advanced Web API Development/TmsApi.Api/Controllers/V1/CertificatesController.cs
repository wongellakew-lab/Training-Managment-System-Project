using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;
using static TmsApi.Api.Diagnostics.Diagnostics;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CertificatesController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly TmsDbContext _context;

    public CertificatesController(ICertificateService certificateService, TmsDbContext context)
    {
        _certificateService = certificateService;
        _context = context;
    }

    public record IssueCertificateRequest(int StudentId, int CourseId);

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("issue")]
    public async Task<IActionResult> IssueCertificate([FromBody] IssueCertificateRequest request, CancellationToken cancellationToken)
    {
        var success = await _certificateService.IssueCertificateAsync(request.StudentId, request.CourseId, cancellationToken);
        if (!success)
        {
            return BadRequest(new { message = "Unable to issue certificate. Please verify student GPA and course completion." });
        }

        CertificatesCounter.Add(1);

        return Ok(new { message = "Certificate issued successfully." });
    }

    [HttpGet("verify/{certificateNumber}")]
    public async Task<IActionResult> VerifyCertificate(string certificateNumber, CancellationToken cancellationToken)
    {
        var certificate = await _context.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, cancellationToken);

        if (certificate == null)
        {
            return NotFound(new { valid = false, message = "Invalid certificate number." });
        }

        var student = await _context.Students.FindAsync(new object[] { certificate.StudentId }, cancellationToken);
        var course = await _context.Courses.FindAsync(new object[] { certificate.CourseId }, cancellationToken);

        return Ok(new
        {
            valid = true,
            certificateNumber = certificate.CertificateNumber,
            issueDate = certificate.IssueDate,
            studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown",
            courseTitle = course?.Title ?? "Unknown"
        });
    }
}
