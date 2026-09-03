using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Extensions;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/student/my-courses")]
[Authorize(Roles = "Student,Admin")]
public class StudentCoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly TmsDbContext _context;
    private readonly IHubContext<TmsHub, ITmsClient> _hubContext;

    public StudentCoursesController(
        ICourseService courseService,
        TmsDbContext context,
        IHubContext<TmsHub, ITmsClient> hubContext)
    {
        _courseService = courseService;
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCourses(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        var courses = await _courseService.GetStudentEnrolledCoursesAsync(studentId.Value, cancellationToken);
        return Ok(courses);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetMyCoursesStatistics(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        var statistics = await _courseService.GetStudentCoursesStatisticsAsync(studentId.Value, cancellationToken);
        return Ok(statistics);
    }

    [HttpPost("{courseId:int}/withdraw")]
    public async Task<IActionResult> WithdrawFromCourse(int courseId, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        var enrollment = await _context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.StudentId == studentId.Value && e.CourseId == courseId && !e.IsDeleted, cancellationToken);

        if (enrollment == null)
        {
            return NotFound(new { message = "You are not enrolled in this course." });
        }

        var course = enrollment.Course;
        if (course == null)
        {
            return NotFound(new { message = "Course not found." });
        }

        // Validate: Withdrawal is strictly rejected when the course has already started
        if (course.StartDate.Date <= DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Students can only withdraw from a course before it starts." });
        }

        // Change enrollment to Withdrawn/Dropped state
        enrollment.Status = EnrollmentStatus.Dropped;
        await _context.SaveChangesAsync(cancellationToken);

        // Recalculate remaining capacity
        var enrolledCount = await _context.Enrollments
            .CountAsync(e => e.CourseId == courseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Pending) && !e.IsDeleted, cancellationToken);

        course.EnrolledStudentsCount = enrolledCount;
        await _context.SaveChangesAsync(cancellationToken);

        // Emit real-time updates for schedule and capacity
        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "withdrawn", studentId = studentId.Value, courseId });
        await _hubContext.Clients.All.ReceiveCapacityAlert(new
        {
            courseId = course.Id,
            enrolledCount = enrolledCount,
            maxCapacity = course.MaxCapacity
        });

        return Ok(new
        {
            message = $"Successfully withdrawn from '{course.Title}'.",
            courseId = course.Id,
            status = "Withdrawn"
        });
    }
}
