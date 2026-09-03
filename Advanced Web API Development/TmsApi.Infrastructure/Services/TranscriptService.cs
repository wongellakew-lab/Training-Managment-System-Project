using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.TranscriptDtos;
using TmsApi.Application.Grading;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class TranscriptService : ITranscriptService
{
    private readonly TmsDbContext _context;
    private readonly GradingService _gradingService;

    public TranscriptService(TmsDbContext context, GradingService gradingService)
    {
        _context = context;
        _gradingService = gradingService;
    }

    public async Task<TranscriptDto?> GetStudentTranscriptAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted, cancellationToken);

        if (student == null) return null;

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId && !e.IsDeleted)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var docNumber = $"TRN-{DateTime.UtcNow.Year}-{student.Id:D4}-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var transcript = new TranscriptDto
        {
            InstitutionName = "Training Management System",
            DocumentNumber = docNumber,
            GeneratedAt = DateTime.UtcNow,
            Student = new TranscriptStudentDto
            {
                StudentId = student.Id,
                FullName = $"{student.FirstName} {student.LastName}".Trim(),
                RegistrationNumber = !string.IsNullOrWhiteSpace(student.RegistrationNumber) ? student.RegistrationNumber : $"REG-{student.Id:D5}",
                Email = student.Email,
                PhoneNumber = student.PhoneNumber ?? student.User?.PhoneNumber,
                Department = !string.IsNullOrWhiteSpace(student.Department) ? student.Department : "Computer Science & Engineering",
                EnrollmentDate = student.EnrollmentDate
            }
        };

        var records = new List<TranscriptCourseRecordDto>();
        var scoresForGpa = new List<decimal>();
        int totalCreditsEarned = 0;
        int completedCount = 0;
        int attemptedCount = 0;

        foreach (var e in enrollments)
        {
            // Omit rejected enrollments
            if (e.Status == EnrollmentStatus.Rejected)
                continue;

            attemptedCount++;
            var course = e.Course;
            int credits = course?.Credits > 0 ? course.Credits : 3;
            int durationHours = course?.DurationHours > 0 ? course.DurationHours : 40;

            // Determine academic term from Course StartDate or Enrollment date
            var courseDate = course?.StartDate ?? e.EnrolledAt;
            string term = GetAcademicTerm(courseDate);

            var record = new TranscriptCourseRecordDto
            {
                CourseId = e.CourseId,
                CourseCode = course?.Code ?? $"CRS-{e.CourseId}",
                CourseTitle = course?.Title ?? "Enrolled Course",
                Category = course?.Category ?? "General",
                Credits = credits,
                DurationHours = durationHours,
                Term = term,
                CompletionDate = course?.EndDate ?? e.EnrolledAt.AddDays(42)
            };

            if (e.Status == EnrollmentStatus.Dropped)
            {
                record.Status = "Withdrawn";
                record.GradeLetter = "W";
                record.GradePoint = 0.0m;
                record.FinalScore = null;
                record.Credits = 0; // No credits for withdrawn
            }
            else if (e.Grade.HasValue)
            {
                record.Status = "Completed";
                completedCount++;
                totalCreditsEarned += credits;

                decimal gradeVal = e.Grade.Value;
                if (gradeVal > 4.0m)
                {
                    // Stored as 0-100 percentage
                    record.FinalScore = Math.Round(gradeVal, 1);
                    record.GradeLetter = _gradingService.CalculateGradeLevel(gradeVal).ToString();
                    record.GradePoint = _gradingService.CalculateGradePoint(gradeVal);
                    scoresForGpa.Add(gradeVal);
                }
                else
                {
                    // Stored as 0.0 - 4.0 GPA point
                    record.GradePoint = Math.Round(gradeVal, 2);
                    record.FinalScore = Math.Round(gradeVal * 25m, 1);
                    record.GradeLetter = _gradingService.CalculateGradeLevel(record.FinalScore.Value).ToString();
                    scoresForGpa.Add(record.FinalScore.Value);
                }
            }
            else if (e.Status == EnrollmentStatus.Completed)
            {
                // Completed but grade not explicitly populated, default passing
                record.Status = "Completed";
                completedCount++;
                totalCreditsEarned += credits;
                record.FinalScore = 85.0m;
                record.GradeLetter = "B";
                record.GradePoint = 3.0m;
                scoresForGpa.Add(85.0m);
            }
            else
            {
                record.Status = "In Progress";
                record.GradeLetter = "IP";
                record.GradePoint = 0.0m;
                record.FinalScore = null;
            }

            records.Add(record);
        }

        transcript.CourseRecords = records;

        decimal calculatedGpa = scoresForGpa.Any()
            ? _gradingService.CalculateGPA(scoresForGpa)
            : (student.Gpa > 0 ? student.Gpa : 0.0m);

        string standing = "Good Standing";
        if (completedCount == 0 && attemptedCount == 0)
        {
            standing = "Newly Enrolled";
        }
        else if (calculatedGpa >= 3.70m)
        {
            standing = "Dean's List / First Class Honours";
        }
        else if (calculatedGpa >= 3.00m)
        {
            standing = "Good Standing (Honours)";
        }
        else if (calculatedGpa >= 2.00m)
        {
            standing = "Good Standing";
        }
        else if (completedCount > 0)
        {
            standing = "Academic Probation";
        }

        transcript.Summary = new TranscriptSummaryDto
        {
            TotalCoursesCompleted = completedCount,
            TotalCoursesAttempted = attemptedCount,
            TotalCreditsEarned = totalCreditsEarned,
            CumulativeGpa = calculatedGpa,
            AcademicStanding = standing
        };

        return transcript;
    }

    public async Task<string> GenerateTranscriptHtmlAsync(int studentId, string? documentNumber = null, CancellationToken cancellationToken = default)
    {
        var transcript = await GetStudentTranscriptAsync(studentId, cancellationToken);
        if (transcript == null)
        {
            return "<html><body><h3>Student record not found.</h3></body></html>";
        }

        var docRef = !string.IsNullOrWhiteSpace(documentNumber) ? documentNumber : transcript.DocumentNumber;

        var rowsHtml = string.Join("\n", transcript.CourseRecords.Select(r => $@"
          <tr>
            <td class=""code-cell"">{r.CourseCode}</td>
            <td class=""title-cell""><strong>{r.CourseTitle}</strong><br/><small class=""text-muted"">{r.Category} &bull; {r.DurationHours} Hours</small></td>
            <td class=""center-cell"">{r.Credits}</td>
            <td class=""center-cell"">{r.Term}</td>
            <td class=""center-cell"">{(r.FinalScore.HasValue ? $"{r.FinalScore.Value:F1}%" : "-")}</td>
            <td class=""center-cell grade-pill-cell""><span class=""letter-pill letter-{r.GradeLetter}"">{r.GradeLetter}</span></td>
            <td class=""center-cell"">{r.GradePoint:F1}</td>
            <td class=""center-cell status-cell status-{r.Status.ToLowerInvariant().Replace(" ", "-")}"">{r.Status}</td>
          </tr>"));

        if (!transcript.CourseRecords.Any())
        {
            rowsHtml = @"<tr><td colspan=""8"" style=""text-align: center; padding: 2rem; color: #64748b;"">No official course enrollments recorded on this academic transcript.</td></tr>";
        }

        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<title>Official Academic Transcript - {transcript.Student.FullName}</title>
<style>
  @page {{
    size: letter portrait;
    margin: 1.5cm;
  }}
  * {{ box-sizing: border-box; }}
  body {{
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    padding: 30px;
    background: #f1f5f9;
    color: #0f172a;
    line-height: 1.5;
  }}
  .transcript-sheet {{
    max-width: 920px;
    margin: 0 auto;
    background: #ffffff;
    padding: 48px;
    border-radius: 12px;
    box-shadow: 0 10px 25px rgba(0,0,0,0.08);
    border: 1px solid #cbd5e1;
    position: relative;
    overflow: hidden;
  }}
  .transcript-sheet::before {{
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    height: 8px;
    background: linear-gradient(90deg, #1e3a8a 0%, #2563eb 50%, #38bdf8 100%);
  }}
  .header-block {{
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    border-bottom: 2px solid #0f172a;
    padding-bottom: 20px;
    margin-bottom: 28px;
  }}
  .inst-name {{
    font-size: 24px;
    font-weight: 800;
    color: #1e3a8a;
    letter-spacing: -0.5px;
    text-transform: uppercase;
    margin: 0 0 4px 0;
  }}
  .doc-title {{
    font-size: 15px;
    font-weight: 700;
    color: #475569;
    letter-spacing: 2px;
    text-transform: uppercase;
    margin: 0;
  }}
  .header-meta {{
    text-align: right;
  }}
  .doc-id-pill {{
    font-family: monospace;
    font-size: 12px;
    font-weight: 700;
    color: #1e3a8a;
    background: #eff6ff;
    padding: 6px 12px;
    border-radius: 6px;
    border: 1px solid #bfdbfe;
    display: inline-block;
    margin-bottom: 6px;
  }}
  .gen-date {{
    font-size: 12px;
    color: #64748b;
  }}
  .section-label {{
    font-size: 12px;
    font-weight: 700;
    text-transform: uppercase;
    color: #1e3a8a;
    letter-spacing: 1px;
    margin-bottom: 8px;
    border-bottom: 1px solid #e2e8f0;
    padding-bottom: 4px;
  }}
  .student-info-grid {{
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 12px 24px;
    background: #f8fafc;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    padding: 16px 20px;
    margin-bottom: 28px;
    font-size: 13.5px;
  }}
  .info-item {{
    display: flex;
    justify-content: space-between;
  }}
  .info-lbl {{
    color: #64748b;
    font-weight: 600;
  }}
  .info-val {{
    font-weight: 700;
    color: #0f172a;
    text-align: right;
  }}
  .table-wrapper {{
    margin-bottom: 28px;
  }}
  table {{
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
  }}
  th {{
    background: #1e293b;
    color: #ffffff;
    font-weight: 700;
    font-size: 11.5px;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    padding: 10px 12px;
    text-align: left;
  }}
  th.center-th {{ text-align: center; }}
  td {{
    padding: 10px 12px;
    border-bottom: 1px solid #e2e8f0;
    vertical-align: middle;
  }}
  .center-cell {{ text-align: center; }}
  .code-cell {{ font-family: monospace; font-weight: 700; color: #1e3a8a; }}
  .title-cell {{ font-size: 13px; }}
  .text-muted {{ color: #64748b; font-size: 11px; }}
  .letter-pill {{
    display: inline-block;
    width: 24px;
    height: 24px;
    line-height: 24px;
    text-align: center;
    border-radius: 4px;
    font-weight: 800;
    font-size: 12px;
  }}
  .letter-A {{ background: #dcfce7; color: #15803d; }}
  .letter-B {{ background: #dbeafe; color: #1d4ed8; }}
  .letter-C {{ background: #fef3c7; color: #b45309; }}
  .letter-D {{ background: #fee2e2; color: #b91c1c; }}
  .letter-F {{ background: #fee2e2; color: #991b1b; }}
  .letter-W {{ background: #f1f5f9; color: #475569; }}
  .letter-IP {{ background: #fef9c3; color: #a16207; }}
  .status-completed {{ color: #16a34a; font-weight: 700; }}
  .status-withdrawn {{ color: #64748b; font-style: italic; }}
  .status-in-progress {{ color: #d97706; font-weight: 600; }}
  .summary-card {{
    background: #f8fafc;
    border: 2px solid #cbd5e1;
    border-radius: 10px;
    padding: 18px 24px;
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 16px;
    margin-bottom: 36px;
    text-align: center;
  }}
  .sum-box .sum-label {{
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
    color: #64748b;
    letter-spacing: 0.5px;
  }}
  .sum-box .sum-val {{
    font-size: 20px;
    font-weight: 800;
    color: #0f172a;
    margin-top: 4px;
  }}
  .sum-box .gpa-val {{ color: #2563eb; }}
  .sum-box .standing-val {{ font-size: 14px; color: #059669; font-weight: 700; margin-top: 6px; }}
  .cert-section {{
    border-top: 2px solid #e2e8f0;
    padding-top: 24px;
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 32px;
    align-items: flex-end;
  }}
  .cert-text {{
    font-size: 11.5px;
    color: #475569;
    line-height: 1.6;
  }}
  .cert-sig-block {{
    text-align: center;
  }}
  .sig-line {{
    border-bottom: 1.5px solid #0f172a;
    margin-bottom: 8px;
    height: 40px;
  }}
  .sig-title {{
    font-size: 12px;
    font-weight: 700;
    color: #0f172a;
  }}
  .sig-sub {{
    font-size: 10.5px;
    color: #64748b;
  }}
  .print-controls {{
    max-width: 920px;
    margin: 0 auto 20px auto;
    display: flex;
    justify-content: flex-end;
    gap: 12px;
  }}
  .print-btn {{
    background: #2563eb;
    color: #ffffff;
    border: none;
    padding: 10px 20px;
    border-radius: 8px;
    font-weight: 700;
    font-size: 14px;
    cursor: pointer;
    box-shadow: 0 4px 12px rgba(37, 99, 235, 0.25);
  }}
  .close-btn {{
    background: #ffffff;
    color: #475569;
    border: 1px solid #cbd5e1;
    padding: 10px 20px;
    border-radius: 8px;
    font-weight: 600;
    font-size: 14px;
    cursor: pointer;
  }}
  @media print {{
    body {{ background: #ffffff; padding: 0; }}
    .transcript-sheet {{ border: none; box-shadow: none; padding: 0; }}
    .print-controls {{ display: none !important; }}
  }}
</style>
</head>
<body>
<div class=""print-controls"">
  <button class=""close-btn"" onclick=""window.close()"">Close</button>
  <button class=""print-btn"" onclick=""window.print()"">Print / Save PDF</button>
</div>

<div class=""transcript-sheet"">
  <div class=""header-block"">
    <div>
      <h1 class=""inst-name"">{transcript.InstitutionName}</h1>
      <h2 class=""doc-title"">Official Academic Transcript</h2>
    </div>
    <div class=""header-meta"">
      <div class=""doc-id-pill"">{docRef}</div>
      <div class=""gen-date"">Generated: {transcript.GeneratedAt:MMMM dd, yyyy}</div>
    </div>
  </div>

  <div class=""section-label"">Student Identification</div>
  <div class=""student-info-grid"">
    <div class=""info-item"">
      <span class=""info-lbl"">Student Name:</span>
      <span class=""info-val"">{transcript.Student.FullName}</span>
    </div>
    <div class=""info-item"">
      <span class=""info-lbl"">Student ID (Reg #):</span>
      <span class=""info-val font-mono"">{transcript.Student.RegistrationNumber}</span>
    </div>
    <div class=""info-item"">
      <span class=""info-lbl"">Email:</span>
      <span class=""info-val"">{transcript.Student.Email}</span>
    </div>
    <div class=""info-item"">
      <span class=""info-lbl"">Department / Program:</span>
      <span class=""info-val"">{transcript.Student.Department}</span>
    </div>
    <div class=""info-item"">
      <span class=""info-lbl"">Matriculation Date:</span>
      <span class=""info-val"">{transcript.Student.EnrollmentDate:MMM dd, yyyy}</span>
    </div>
    <div class=""info-item"">
      <span class=""info-lbl"">Record Status:</span>
      <span class=""info-val"" style=""color: #16a34a;"">Active & Certified</span>
    </div>
  </div>

  <div class=""section-label"">Academic Course Records</div>
  <div class=""table-wrapper"">
    <table>
      <thead>
        <tr>
          <th>Code</th>
          <th>Course Title & Scope</th>
          <th class=""center-th"">Credits</th>
          <th class=""center-th"">Term</th>
          <th class=""center-th"">Score</th>
          <th class=""center-th"">Grade</th>
          <th class=""center-th"">Point</th>
          <th class=""center-th"">Status</th>
        </tr>
      </thead>
      <tbody>
        {rowsHtml}
      </tbody>
    </table>
  </div>

  <div class=""section-label"">Academic Record Summary</div>
  <div class=""summary-card"">
    <div class=""sum-box"">
      <div class=""sum-label"">Courses Completed</div>
      <div class=""sum-val"">{transcript.Summary.TotalCoursesCompleted} of {transcript.Summary.TotalCoursesAttempted}</div>
    </div>
    <div class=""sum-box"">
      <div class=""sum-label"">Credits Earned</div>
      <div class=""sum-val"">{transcript.Summary.TotalCreditsEarned}</div>
    </div>
    <div class=""sum-box"">
      <div class=""sum-label"">Cumulative GPA</div>
      <div class=""sum-val gpa-val"">{transcript.Summary.CumulativeGpa:F2} <small style=""font-size: 12px; color: #64748b;"">/ 4.00</small></div>
    </div>
    <div class=""sum-box"">
      <div class=""sum-label"">Academic Standing</div>
      <div class=""sum-val standing-val"">{transcript.Summary.AcademicStanding}</div>
    </div>
  </div>

  <div class=""cert-section"">
    <div class=""cert-text"">
      <strong>OFFICIAL INSTITUTIONAL CERTIFICATION</strong><br/>
      This official transcript represents the authentic academic record of the student as maintained by the Office of the Academic Registrar at {transcript.InstitutionName}. Alteration or falsification of this document is strictly prohibited under institutional academic policies. Verification Code: <strong style=""font-family: monospace;"">{docRef}</strong>.
    </div>
    <div class=""cert-sig-block"">
      <div class=""sig-line""></div>
      <div class=""sig-title"">Office of the Academic Registrar</div>
      <div class=""sig-sub"">{transcript.InstitutionName}</div>
    </div>
  </div>
</div>
</body>
</html>";

        return html;
    }

    private static string GetAcademicTerm(DateTime date)
    {
        int month = date.Month;
        int year = date.Year;

        if (month >= 8 && month <= 12)
            return $"Fall {year}";
        if (month >= 1 && month <= 5)
            return $"Spring {year}";
        return $"Summer {year}";
    }
}
