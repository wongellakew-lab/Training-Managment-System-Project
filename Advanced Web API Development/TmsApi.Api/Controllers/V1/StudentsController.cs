using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly TmsDbContext _context;
    private readonly UserManager<TmsUser> _userManager;

    public StudentsController(IStudentService studentService, TmsDbContext context, UserManager<TmsUser> userManager)
    {
        _studentService = studentService;
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var response = await _studentService.GetStudentsAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStudentStats(CancellationToken cancellationToken)
    {
        var totalStudents = await _context.Students.CountAsync(s => !s.IsDeleted, cancellationToken);
        var activeEnrolled = await _context.Enrollments
            .Where(e => e.Status == EnrollmentStatus.Active)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);
        var certificatesIssued = await _context.Certificates.CountAsync(cancellationToken);

        return Ok(new StudentStatsDto
        {
            TotalStudents = totalStudents,
            ActiveEnrolled = activeEnrolled,
            CertificatesIssued = certificatesIssued
        });
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStudentById(int id, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var currentStudentId = await User.ResolveStudentIdAsync(_context);
            if (!currentStudentId.HasValue || currentStudentId.Value != id)
            {
                return Forbid();
            }
        }

        var student = await _studentService.GetStudentByIdAsync(id, cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        return Ok(student);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto, CancellationToken cancellationToken)
    {
        var student = await _studentService.CreateStudentAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetStudentById), new { version = "1.0", id = student.Id }, student);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto, CancellationToken cancellationToken)
    {
        var updated = await _studentService.UpdateStudentAsync(id, dto, cancellationToken);
        if (!updated) return NotFound(new { message = $"Student with ID {id} not found." });

        return NoContent();
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{id:int}/detail")]
    public async Task<IActionResult> GetStudentDetail(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Instructor)
            .Include(s => s.Assessments)
            .SingleOrDefaultAsync(st => st.Id == id && !st.IsDeleted, cancellationToken);

        if (student == null)
            return NotFound(new { message = $"Student with ID {id} not found." });

        var enrolledCourseIds = student.Enrollments
            .Where(e => !e.IsDeleted && e.Course != null && !e.Course.IsDeleted)
            .Select(e => e.CourseId)
            .ToList();

        var assignmentsCountByCourse = await _context.Assignments
            .Where(a => enrolledCourseIds.Contains(a.CourseId) && !a.IsDeleted)
            .GroupBy(a => a.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);

        var enrolledCourses = student.Enrollments
            .Where(e => !e.IsDeleted && e.Course != null && !e.Course.IsDeleted)
            .Select(e =>
            {
                var course = e.Course!;
                var totalAssignments = assignmentsCountByCourse.GetValueOrDefault(course.Id, 0);
                var completedAssignments = student.Assessments?
                    .Count(a => a.CourseId == course.Id && a.AssignmentId != null && a.Status == "Graded" && a.ScoredPoints.HasValue) ?? 0;

                decimal progress = totalAssignments > 0
                    ? Math.Min(100m, Math.Round(((decimal)completedAssignments / totalAssignments) * 100m, 0))
                    : 0m;

                if (totalAssignments > 0 && completedAssignments < totalAssignments && progress >= 100m)
                {
                    progress = 99m;
                }

                var gradedScores = student.Assessments?
                    .Where(a => a.CourseId == course.Id && a.Status == "Graded" && a.ScoredPoints.HasValue)
                    .Select(a => a.ScoredPoints!.Value)
                    .ToList();
                decimal? avgScore = gradedScores != null && gradedScores.Count > 0 ? Math.Round(gradedScores.Average(), 1) : null;

                return new AdminStudentEnrolledCourseDto
                {
                    CourseId = course.Id,
                    Code = course.Code,
                    Title = course.Title,
                    Category = course.Category,
                    Level = course.Level,
                    ImageUrl = course.ImageUrl,
                    DurationHours = course.DurationHours,
                    LessonsCount = course.LessonsCount,
                    StartDate = course.StartDate,
                    EndDate = course.EndDate,
                    EnrollmentStatus = e.Status.ToString(),
                    EnrolledAt = e.EnrolledAt,
                    ProgressPercentage = progress,
                    FinalScore = avgScore,
                    InstructorName = course.Instructor != null ? $"{course.Instructor.FirstName} {course.Instructor.LastName}".Trim() : "TBA"
                };
            }).ToList();

        var detailDto = new AdminStudentDetailDto
        {
            Id = student.Id,
            RegistrationNumber = student.RegistrationNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            PhoneNumber = student.PhoneNumber ?? student.User?.PhoneNumber,
            Department = student.Department ?? "General Studies",
            Bio = student.Bio ?? student.User?.Bio,
            EnrollmentDate = student.EnrollmentDate,
            Gpa = student.Gpa,
            Status = !string.IsNullOrWhiteSpace(student.Status) ? student.Status : "Active",
            Avatar = !string.IsNullOrEmpty(student.ProfilePictureUrl)
                ? student.ProfilePictureUrl
                : (!string.IsNullOrEmpty(student.User?.ProfilePictureUrl)
                    ? student.User.ProfilePictureUrl
                    : "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80"),
            EnrolledCourses = enrolledCourses
        };

        return Ok(detailDto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/suspend")]
    public async Task<IActionResult> SuspendStudent(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        student.Status = "Suspended";

        if (!string.IsNullOrEmpty(student.UserId))
        {
            var user = await _userManager.FindByIdAsync(student.UserId);
            if (user != null)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Student account suspended successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/probation")]
    public async Task<IActionResult> PlaceOnProbation(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        student.Status = "Probation";

        if (!string.IsNullOrEmpty(student.UserId))
        {
            var user = await _userManager.FindByIdAsync(student.UserId);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Student placed on probation successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> ActivateStudent(int id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (student == null) return NotFound(new { message = $"Student with ID {id} not found." });

        student.Status = "Active";

        if (!string.IsNullOrEmpty(student.UserId))
        {
            var user = await _userManager.FindByIdAsync(student.UserId);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Student account reactivated successfully." });
    }
}
