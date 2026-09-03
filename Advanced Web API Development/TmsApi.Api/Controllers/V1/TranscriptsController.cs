using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Api.Extensions;
using static TmsApi.Api.Diagnostics.Diagnostics;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TranscriptsController : ControllerBase
{
    private readonly TranscriptQueue _queue;
    private readonly IStudentService _studentService;
    private readonly TmsDbContext _context;

    public TranscriptsController(TranscriptQueue queue, IStudentService studentService, TmsDbContext context)
    {
        _queue = queue;
        _studentService = studentService;
        _context = context;
    }

    public record RequestTranscriptDto(int? StudentId);

    [Authorize]
    [HttpPost("request")]
    public async Task<IActionResult> RequestTranscript([FromBody] RequestTranscriptDto dto, CancellationToken cancellationToken)
    {
        int targetStudentId = dto.StudentId ?? 0;
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var myStudentId = await User.ResolveStudentIdAsync(_context);
            if (!myStudentId.HasValue)
                return Unauthorized(new { message = "Student record not associated with authenticated user." });
            targetStudentId = myStudentId.Value;
        }

        var student = await _studentService.GetStudentByIdAsync(targetStudentId, cancellationToken);
        if (student == null)
        {
            return NotFound(new { message = $"Student with ID {targetStudentId} not found." });
        }

        var requestId = Guid.NewGuid().ToString("N");
        var request = new TranscriptRequest(
            requestId,
            student.Id,
            $"{student.FirstName} {student.LastName}",
            student.Email
        );

        await _queue.QueueTranscriptAsync(request, cancellationToken);
        TranscriptsCounter.Add(1);

        return Accepted(new
        {
            requestId,
            status = "Processing",
            message = "Transcript generation has been queued. You will receive a real-time notification upon completion."
        });
    }

    [Authorize]
    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> DownloadTranscript(string fileName, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue) return Unauthorized(new { message = "User not associated with a student account." });

        var student = await _studentService.GetStudentByIdAsync(studentId.Value, cancellationToken);
        if (student == null) return NotFound(new { message = "Student record not found." });

        var studentEntity = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId.Value, cancellationToken);

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId.Value && !e.IsDeleted)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var rowsHtml = string.Join("\n", enrollments.Select(e => $@"
          <tr>
            <td>{e.Course?.Code ?? "CRS"}</td>
            <td>{e.Course?.Title ?? "Course"}</td>
            <td>{(e.Course?.Credits > 0 ? e.Course.Credits : 3)}</td>
            <td>{(e.Grade.HasValue ? e.Grade.Value.ToString("F2") : "In Progress")}</td>
            <td>{(e.Status.ToString())}</td>
          </tr>"));

        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<title>Official Academic Transcript - {student.FirstName} {student.LastName}</title>
<style>
  body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; padding: 40px; background: #f8fafc; color: #1e293b; }}
  .transcript-container {{ max-width: 800px; margin: 0 auto; background: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 4px 15px rgba(0,0,0,0.06); border-top: 6px solid #0284c7; }}
  .header {{ display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; padding-bottom: 16px; border-bottom: 2px solid #e2e8f0; }}
  .univ-title {{ font-size: 24px; font-weight: bold; color: #0284c7; }}
  .doc-title {{ font-size: 14px; text-transform: uppercase; color: #64748b; letter-spacing: 1px; }}
  .student-info {{ display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 24px; font-size: 14px; }}
  .label {{ color: #64748b; font-weight: 500; }}
  .val {{ font-weight: 600; color: #0f172a; }}
  table {{ width: 100%; border-collapse: collapse; margin-bottom: 32px; font-size: 14px; }}
  th {{ background: #f1f5f9; text-align: left; padding: 10px 12px; font-weight: 600; color: #334155; border-bottom: 1px solid #cbd5e1; }}
  td {{ padding: 10px 12px; border-bottom: 1px solid #e2e8f0; }}
  .footer {{ display: flex; justify-content: space-between; align-items: center; border-top: 1px solid #e2e8f0; padding-top: 20px; font-size: 13px; color: #64748b; }}
  .gpa-pill {{ display: inline-block; background: #e0f2fe; color: #0369a1; padding: 4px 12px; border-radius: 9999px; font-weight: bold; font-size: 14px; }}
  .print-btn {{ position: fixed; top: 20px; right: 20px; background: #0284c7; color: white; border: none; padding: 10px 18px; border-radius: 6px; font-weight: 600; cursor: pointer; }}
  @media print {{ .print-btn {{ display: none; }} body {{ padding: 0; background: white; }} .transcript-container {{ box-shadow: none; }} }}
</style>
</head>
<body>
<button class=""print-btn"" onclick=""window.print()"">Print / Save PDF</button>
<div class=""transcript-container"">
  <div class=""header"">
    <div>
      <div class=""univ-title"">Training Management System</div>
      <div class=""doc-title"">Official Academic Record</div>
    </div>
    <div style=""text-align: right;"">
      <div class=""gpa-pill"">GPA: {student.Gpa:F2} / 4.00</div>
    </div>
  </div>
  <div class=""student-info"">
    <div><span class=""label"">Student Name: </span><span class=""val"">{student.FirstName} {student.LastName}</span></div>
    <div><span class=""label"">Student ID: </span><span class=""val"">{student.RegistrationNumber}</span></div>
    <div><span class=""label"">Email: </span><span class=""val"">{student.Email}</span></div>
    <div><span class=""label"">Department: </span><span class=""val"">{studentEntity?.Department ?? "General Academic Program"}</span></div>
    <div><span class=""label"">Issue Date: </span><span class=""val"">{DateTime.UtcNow:MMMM dd, yyyy}</span></div>
    <div><span class=""label"">Document ID: </span><span class=""val"">{fileName}</span></div>
  </div>
  <table>
    <thead>
      <tr>
        <th>Code</th>
        <th>Course Title</th>
        <th>Credits</th>
        <th>Grade (GPA)</th>
        <th>Status</th>
      </tr>
    </thead>
    <tbody>
      {rowsHtml}
    </tbody>
  </table>
  <div class=""footer"">
    <div>Office of the Academic Registrar &bull; Official Digital Record</div>
    <div>Status: Valid & Certified</div>
  </div>
</div>
</body>
</html>";

        return Content(html, "text/html");
    }
}
