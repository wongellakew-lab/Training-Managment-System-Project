using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.GradeDtos;
using TmsApi.Application.Grading;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class GradesController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly GradingService _gradingService;

    public GradesController(TmsDbContext context, GradingService gradingService)
    {
        _context = context;
        _gradingService = gradingService;
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("assign")]
    public async Task<IActionResult> AssignGrade([FromBody] GradeDto dto, CancellationToken cancellationToken)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId, cancellationToken);

        if (enrollment == null)
        {
            return NotFound(new { message = $"Enrollment with ID {dto.EnrollmentId} not found." });
        }

        enrollment.Grade = dto.Grade;

        var studentGrades = await _context.Enrollments
            .Where(e => e.StudentId == enrollment.StudentId && e.Grade.HasValue)
            .Select(e => e.Grade!.Value)
            .ToListAsync(cancellationToken);

        studentGrades.Add(dto.Grade);
        enrollment.Student.Gpa = _gradingService.CalculateGPA(studentGrades);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            enrollmentId = enrollment.Id,
            grade = enrollment.Grade,
            updatedGpa = enrollment.Student.Gpa,
            letterGrade = _gradingService.CalculateGradeLevel(dto.Grade).ToString()
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("bulk-assign")]
    public async Task<IActionResult> BulkAssignGrades([FromBody] BulkGradeDto dto, CancellationToken cancellationToken)
    {
        var results = new List<object>();

        foreach (var item in dto.Grades)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .FirstOrDefaultAsync(e => e.Id == item.EnrollmentId, cancellationToken);

            if (enrollment != null)
            {
                enrollment.Grade = item.Grade;
                results.Add(new { enrollmentId = item.EnrollmentId, grade = item.Grade, status = "Updated" });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(results);
    }
}
