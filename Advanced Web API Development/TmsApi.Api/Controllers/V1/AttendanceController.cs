using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly IHubContext<TmsHub, ITmsClient> _hubContext;

    public AttendanceController(TmsDbContext context, IHubContext<TmsHub, ITmsClient> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public record MarkAttendanceRequest(int StudentId, int CourseId, int? ScheduleId, DateTime SessionDate, int Status, string? Remarks);
    public record BulkAttendanceItem(int StudentId, int Status, string? Remarks);
    public record BulkAttendanceRequest(int CourseId, int? ScheduleId, DateTime SessionDate, List<BulkAttendanceItem> Items);

    private async Task<bool> CanManageCourseAttendanceAsync(int courseId)
    {
        if (User?.IsInRole("Admin") == true) return true;
        var instructorId = User != null ? await User.ResolveInstructorIdAsync(_context) : null;
        if (!instructorId.HasValue) return false;

        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        return course != null && course.InstructorId == instructorId.Value;
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("course/{courseId:int}")]
    public async Task<IActionResult> GetCourseAttendance(int courseId, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAttendanceAsync(courseId))
        {
            return Forbid();
        }

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course == null) return NotFound(new { message = $"Course with ID {courseId} not found." });

        var targetDate = (date ?? DateTime.UtcNow).Date;

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
                .ThenInclude(s => s.User)
            .Where(e => e.CourseId == courseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .OrderBy(e => e.Student.LastName)
            .ThenBy(e => e.Student.FirstName)
            .ToListAsync(cancellationToken);

        var existingAttendances = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && a.SessionDate.Date == targetDate)
            .ToDictionaryAsync(a => a.StudentId, cancellationToken);

        // Check if a session was scheduled on this date
        var schedules = await _context.CourseSchedules
            .AsNoTracking()
            .Where(s => s.CourseId == courseId && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        var matchingSchedule = schedules.FirstOrDefault(s => s.DayOfWeek == targetDate.DayOfWeek);
        var hasAttendanceRecords = existingAttendances.Any();

        bool isWithinDateRange = true;
        if (course.StartDate != default && targetDate < course.StartDate.Date) isWithinDateRange = false;
        if (course.EndDate != default && course.EndDate != DateTime.MinValue && targetDate > course.EndDate.Date) isWithinDateRange = false;

        bool hasScheduledSession = hasAttendanceRecords || (matchingSchedule != null && isWithinDateRange);
        bool isRecorded = hasAttendanceRecords;

        var studentItems = enrollments.Select(e =>
        {
            var student = e.Student;
            existingAttendances.TryGetValue(student.Id, out var att);

            return new
            {
                studentId = student.Id,
                firstName = student.FirstName,
                lastName = student.LastName,
                email = student.Email,
                registrationNumber = student.RegistrationNumber,
                avatarUrl = student.User?.ProfilePictureUrl ?? student.ProfilePictureUrl,
                status = att != null ? att.Status.ToString() : "Unmarked",
                statusCode = att != null ? (int)att.Status : 0,
                remarks = att?.Remarks,
                recordedAt = att?.RecordedAt
            };
        }).ToList();

        var presentCount = studentItems.Count(s => s.statusCode == (int)AttendanceStatus.Present);
        var lateCount = studentItems.Count(s => s.statusCode == (int)AttendanceStatus.Late);
        var absentCount = studentItems.Count(s => s.statusCode == (int)AttendanceStatus.Absent);

        return Ok(new
        {
            courseId = course.Id,
            courseTitle = course.Title,
            courseCode = course.Code,
            sessionDate = targetDate,
            hasScheduledSession,
            isRecorded,
            scheduleInfo = matchingSchedule != null ? new
            {
                startTime = matchingSchedule.StartTime,
                endTime = matchingSchedule.EndTime,
                location = matchingSchedule.Location,
                room = matchingSchedule.Room,
                scheduleType = matchingSchedule.ScheduleType
            } : null,
            totalEnrolled = studentItems.Count,
            presentCount,
            lateCount,
            absentCount,
            students = hasScheduledSession ? (IEnumerable<object>)studentItems : Array.Empty<object>()
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("mark")]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAttendanceAsync(request.CourseId))
        {
            return Forbid();
        }

        var instructorId = await User.ResolveInstructorIdAsync(_context);
        var targetDate = request.SessionDate.Date;

        var isEnrolled = await _context.Enrollments
            .AnyAsync(e => e.CourseId == request.CourseId && e.StudentId == request.StudentId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted, cancellationToken);
        if (!isEnrolled)
        {
            return BadRequest(new { message = "Student is not enrolled in this course." });
        }

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.CourseId == request.CourseId &&
                                      a.StudentId == request.StudentId &&
                                      a.SessionDate.Date == targetDate, cancellationToken);

        var status = Enum.IsDefined(typeof(AttendanceStatus), request.Status)
            ? (AttendanceStatus)request.Status
            : AttendanceStatus.Present;

        if (attendance != null)
        {
            attendance.Status = status;
            attendance.Remarks = request.Remarks;
            attendance.ScheduleId = request.ScheduleId ?? attendance.ScheduleId;
            attendance.RecordedAt = DateTime.UtcNow;
            attendance.RecordedByInstructorId = instructorId;
        }
        else
        {
            attendance = new Attendance
            {
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                ScheduleId = request.ScheduleId,
                SessionDate = targetDate,
                Status = status,
                Remarks = request.Remarks,
                RecordedAt = DateTime.UtcNow,
                RecordedByInstructorId = instructorId
            };
            await _context.Attendances.AddAsync(attendance, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.ReceiveAttendanceUpdate(new
        {
            courseId = request.CourseId,
            studentId = request.StudentId,
            status = status.ToString(),
            date = targetDate
        });

        return Ok(new { message = "Attendance marked successfully.", status = status.ToString() });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("bulk")]
    public async Task<IActionResult> SaveBulkAttendance([FromBody] BulkAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAttendanceAsync(request.CourseId))
        {
            return Forbid();
        }

        var instructorId = await User.ResolveInstructorIdAsync(_context);
        var targetDate = request.SessionDate.Date;

        var enrolledStudentIds = await _context.Enrollments
            .Where(e => e.CourseId == request.CourseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);

        var existingList = await _context.Attendances
            .Where(a => a.CourseId == request.CourseId && a.SessionDate.Date == targetDate)
            .ToListAsync(cancellationToken);

        var existingDict = existingList.ToDictionary(a => a.StudentId);

        foreach (var item in request.Items)
        {
            if (!enrolledStudentIds.Contains(item.StudentId)) continue;
            var status = Enum.IsDefined(typeof(AttendanceStatus), item.Status)
                ? (AttendanceStatus)item.Status
                : AttendanceStatus.Present;

            if (existingDict.TryGetValue(item.StudentId, out var existing))
            {
                existing.Status = status;
                existing.Remarks = item.Remarks;
                existing.RecordedAt = DateTime.UtcNow;
                existing.RecordedByInstructorId = instructorId;
            }
            else
            {
                var newAtt = new Attendance
                {
                    StudentId = item.StudentId,
                    CourseId = request.CourseId,
                    ScheduleId = request.ScheduleId,
                    SessionDate = targetDate,
                    Status = status,
                    Remarks = item.Remarks,
                    RecordedAt = DateTime.UtcNow,
                    RecordedByInstructorId = instructorId
                };
                await _context.Attendances.AddAsync(newAtt, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.ReceiveAttendanceUpdate(new
        {
            courseId = request.CourseId,
            count = request.Items.Count,
            date = targetDate
        });

        return Ok(new { message = $"Roll call saved successfully for {request.Items.Count} students." });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("stats/course/{courseId:int}")]
    public async Task<IActionResult> GetCourseAttendanceStats(int courseId, CancellationToken cancellationToken)
    {
        if (!await CanManageCourseAttendanceAsync(courseId))
        {
            return Forbid();
        }

        var attendances = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .ToListAsync(cancellationToken);

        var totalRecords = attendances.Count;
        var presentCount = attendances.Count(a => a.Status == AttendanceStatus.Present);
        var lateCount = attendances.Count(a => a.Status == AttendanceStatus.Late);
        var absentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent);

        var sessionsCount = attendances.Select(a => a.SessionDate.Date).Distinct().Count();
        decimal attendanceRate = totalRecords > 0
            ? Math.Round(((decimal)(presentCount + lateCount) / totalRecords) * 100m, 1)
            : 0m;

        return Ok(new
        {
            courseId,
            sessionsCount,
            totalRecords,
            presentCount,
            lateCount,
            absentCount,
            attendanceRate
        });
    }
}
