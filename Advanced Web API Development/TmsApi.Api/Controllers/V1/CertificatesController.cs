using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Api.Extensions;
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

    [Authorize]
    [HttpGet("my-certificates")]
    public async Task<IActionResult> GetMyCertificates(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue) return Ok(new List<object>());

        var certs = await (from c in _context.Certificates.AsNoTracking()
                           join crs in _context.Courses.AsNoTracking() on c.CourseId equals crs.Id into crsGroup
                           from course in crsGroup.DefaultIfEmpty()
                           join st in _context.Students.AsNoTracking() on c.StudentId equals st.Id into stGroup
                           from student in stGroup.DefaultIfEmpty()
                           where c.StudentId == studentId.Value
                           orderby c.IssueDate descending
                           select new
                           {
                               id = c.Id,
                               certificateCode = c.CertificateNumber,
                               courseId = c.CourseId,
                               courseTitle = course != null ? course.Title : "Course",
                               studentId = c.StudentId,
                               studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Student",
                               issuedAt = c.IssueDate,
                               isValid = true,
                               verificationUrl = $"/api/v1/certificates/verify/{c.CertificateNumber}"
                           }).ToListAsync(cancellationToken);

        return Ok(certs);
    }

    [Authorize]
    [HttpGet("student/{studentId:int}")]
    public async Task<IActionResult> GetStudentCertificates(int studentId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var myStudentId = await User.ResolveStudentIdAsync(_context);
            if (!myStudentId.HasValue || myStudentId.Value != studentId)
                return Forbid();
        }

        var certs = await (from c in _context.Certificates.AsNoTracking()
                           join crs in _context.Courses.AsNoTracking() on c.CourseId equals crs.Id into crsGroup
                           from course in crsGroup.DefaultIfEmpty()
                           join st in _context.Students.AsNoTracking() on c.StudentId equals st.Id into stGroup
                           from student in stGroup.DefaultIfEmpty()
                           where c.StudentId == studentId
                           orderby c.IssueDate descending
                           select new
                           {
                               id = c.Id,
                               certificateCode = c.CertificateNumber,
                               courseId = c.CourseId,
                               courseTitle = course != null ? course.Title : "Course",
                               studentId = c.StudentId,
                               studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Student",
                               issuedAt = c.IssueDate,
                               isValid = true,
                               verificationUrl = $"/api/v1/certificates/verify/{c.CertificateNumber}"
                           }).ToListAsync(cancellationToken);

        return Ok(certs);
    }

    [HttpGet("verify/{certificateNumber}")]
    public async Task<IActionResult> VerifyCertificate(string certificateNumber, CancellationToken cancellationToken)
    {
        var certificate = await _context.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, cancellationToken);

        if (certificate == null)
        {
            return NotFound(new { valid = false, isValid = false, message = "Invalid certificate number." });
        }

        var student = await _context.Students.FindAsync(new object[] { certificate.StudentId }, cancellationToken);
        var course = await _context.Courses.FindAsync(new object[] { certificate.CourseId }, cancellationToken);

        return Ok(new
        {
            id = certificate.Id,
            valid = true,
            isValid = true,
            certificateCode = certificate.CertificateNumber,
            certificateNumber = certificate.CertificateNumber,
            issuedAt = certificate.IssueDate,
            issueDate = certificate.IssueDate,
            studentId = certificate.StudentId,
            studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Student",
            courseId = certificate.CourseId,
            courseTitle = course?.Title ?? "Course",
            verificationUrl = certificate.VerificationUrl
        });
    }

    [Authorize]
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> DownloadCertificate(int id, CancellationToken cancellationToken)
    {
        var certificate = await _context.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (certificate == null) return NotFound(new { message = "Certificate not found." });

        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var myStudentId = await User.ResolveStudentIdAsync(_context);
            if (!myStudentId.HasValue || myStudentId.Value != certificate.StudentId)
                return Forbid();
        }

        var student = await _context.Students.FindAsync(new object[] { certificate.StudentId }, cancellationToken);
        var course = await _context.Courses.FindAsync(new object[] { certificate.CourseId }, cancellationToken);

        var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Valued Student";
        var courseTitle = course?.Title ?? "Course Completion";
        var formattedDate = certificate.IssueDate.ToString("MMMM dd, yyyy");

        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<title>Certificate of Completion - {certificate.CertificateNumber}</title>
<style>
  @page {{ size: landscape; margin: 0; }}
  body {{
    margin: 0;
    padding: 40px;
    font-family: 'Georgia', serif;
    background: #f8fafc;
    color: #1e293b;
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    box-sizing: border-box;
  }}
  .cert-container {{
    width: 900px;
    background: #ffffff;
    border: 12px double #0284c7;
    border-radius: 8px;
    padding: 48px;
    box-shadow: 0 10px 25px rgba(0,0,0,0.08);
    text-align: center;
    position: relative;
  }}
  .cert-header {{
    text-transform: uppercase;
    letter-spacing: 4px;
    font-size: 14px;
    color: #0284c7;
    font-weight: bold;
    margin-bottom: 8px;
  }}
  .cert-title {{
    font-size: 38px;
    font-weight: 700;
    margin: 0 0 16px 0;
    color: #0f172a;
    letter-spacing: -0.5px;
  }}
  .cert-subtitle {{
    font-style: italic;
    color: #64748b;
    font-size: 16px;
    margin-bottom: 24px;
  }}
  .student-name {{
    font-size: 32px;
    font-weight: bold;
    color: #0369a1;
    border-bottom: 2px solid #e2e8f0;
    display: inline-block;
    padding: 0 32px 8px 32px;
    margin-bottom: 24px;
  }}
  .cert-body {{
    font-size: 16px;
    line-height: 1.6;
    color: #334155;
    max-width: 650px;
    margin: 0 auto 36px auto;
  }}
  .course-name {{
    font-weight: bold;
    color: #0f172a;
  }}
  .cert-footer {{
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    margin-top: 48px;
    padding: 0 40px;
  }}
  .sig-block {{
    text-align: center;
  }}
  .sig-line {{
    width: 180px;
    border-top: 1px solid #94a3b8;
    margin-bottom: 6px;
  }}
  .sig-title {{
    font-size: 12px;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 1px;
  }}
  .cert-seal {{
    width: 80px;
    height: 80px;
    border: 3px dashed #f59e0b;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #d97706;
    font-weight: bold;
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: 1px;
    margin: 0 auto;
  }}
  .cert-code {{
    margin-top: 24px;
    font-size: 12px;
    font-family: monospace;
    color: #94a3b8;
  }}
  .print-btn {{
    position: fixed;
    top: 20px;
    right: 20px;
    background: #0284c7;
    color: white;
    border: none;
    padding: 10px 20px;
    border-radius: 6px;
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
  }}
  @media print {{
    .print-btn {{ display: none; }}
    body {{ padding: 0; background: white; }}
    .cert-container {{ box-shadow: none; border-width: 8px; }}
  }}
</style>
</head>
<body>
<button class=""print-btn"" onclick=""window.print()"">Print / Save PDF</button>
<div class=""cert-container"">
  <div class=""cert-header"">Training Management System &bull; Academic Portal</div>
  <h1 class=""cert-title"">Certificate of Completion</h1>
  <div class=""cert-subtitle"">This official certificate is proudly conferred upon</div>
  <div class=""student-name"">{studentName}</div>
  <div class=""cert-body"">
    for successfully completing all academic requirements, practical assessments, and required coursework for
    <div class=""course-name"" style=""font-size: 20px; margin-top: 8px;"">{courseTitle}</div>
  </div>
  <div class=""cert-footer"">
    <div class=""sig-block"">
      <div class=""sig-line""></div>
      <div class=""sig-title"">Academic Dean</div>
    </div>
    <div class=""cert-seal"">Verified<br>Credential</div>
    <div class=""sig-block"">
      <div class=""sig-line""></div>
      <div class=""sig-title"">Date: {formattedDate}</div>
    </div>
  </div>
  <div class=""cert-code"">Official Verification ID: {certificate.CertificateNumber}</div>
</div>
</body>
</html>";

        return Content(html, "text/html");
    }
}
