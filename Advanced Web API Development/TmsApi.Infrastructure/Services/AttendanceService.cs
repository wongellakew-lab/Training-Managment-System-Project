using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.AttendanceDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class AttendanceService(
    TmsDbContext _context,
    IHubContext<TmsHub, ITmsClient> _hubContext) : IAttendanceService
{

    public async Task<CourseAttendanceSessionDto?> GetCourseAttendanceSessionAsync(int courseId, DateTime? date, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course == null) return null;

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

            return new StudentAttendanceItemDto
            {
                StudentId = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                RegistrationNumber = student.RegistrationNumber,
                AvatarUrl = student.User?.ProfilePictureUrl ?? student.ProfilePictureUrl,
                Status = att != null ? att.Status.ToString() : "Unmarked",
                StatusCode = att != null ? (int)att.Status : 0,
                Remarks = att?.Remarks,
                RecordedAt = att?.RecordedAt
            };
        }).ToList();

        var presentCount = studentItems.Count(s => s.StatusCode == (int)AttendanceStatus.Present);
        var lateCount = studentItems.Count(s => s.StatusCode == (int)AttendanceStatus.Late);
        var absentCount = studentItems.Count(s => s.StatusCode == (int)AttendanceStatus.Absent);

        return new CourseAttendanceSessionDto
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            CourseCode = course.Code,
            SessionDate = targetDate,
            HasScheduledSession = hasScheduledSession,
            IsRecorded = isRecorded,
            ScheduleInfo = matchingSchedule != null ? new ScheduleInfoDto
            {
                StartTime = matchingSchedule.StartTime,
                EndTime = matchingSchedule.EndTime,
                Location = matchingSchedule.Location,
                Room = matchingSchedule.Room,
                ScheduleType = matchingSchedule.ScheduleType
            } : null,
            TotalEnrolled = studentItems.Count,
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            Students = hasScheduledSession ? studentItems : new List<StudentAttendanceItemDto>()
        };
    }

    public async Task<bool> MarkAttendanceAsync(int studentId, int courseId, int? scheduleId, DateTime sessionDate, int status, string? remarks, int? recordedByInstructorId, CancellationToken cancellationToken = default)
    {
        var isEnrolled = await _context.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted, cancellationToken);
        if (!isEnrolled) return false;

        var targetDate = sessionDate.Date;
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.CourseId == courseId &&
                                      a.StudentId == studentId &&
                                      a.SessionDate.Date == targetDate, cancellationToken);

        var attStatus = Enum.IsDefined(typeof(AttendanceStatus), status)
            ? (AttendanceStatus)status
            : AttendanceStatus.Present;

        if (attendance != null)
        {
            attendance.Status = attStatus;
            attendance.Remarks = remarks;
            attendance.ScheduleId = scheduleId ?? attendance.ScheduleId;
            attendance.RecordedAt = DateTime.UtcNow;
            attendance.RecordedByInstructorId = recordedByInstructorId;
        }
        else
        {
            attendance = new Attendance
            {
                StudentId = studentId,
                CourseId = courseId,
                ScheduleId = scheduleId,
                SessionDate = targetDate,
                Status = attStatus,
                Remarks = remarks,
                RecordedAt = DateTime.UtcNow,
                RecordedByInstructorId = recordedByInstructorId
            };
            await _context.Attendances.AddAsync(attendance, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.ReceiveAttendanceUpdate(new
        {
            courseId,
            studentId,
            status = attStatus.ToString(),
            date = targetDate
        });

        return true;
    }

    public async Task<int> SaveBulkAttendanceAsync(BulkAttendanceRequestDto request, int? recordedByInstructorId, CancellationToken cancellationToken = default)
    {
        var targetDate = request.SessionDate.Date;

        var enrolledStudentIds = await _context.Enrollments
            .Where(e => e.CourseId == request.CourseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);

        var existingList = await _context.Attendances
            .Where(a => a.CourseId == request.CourseId && a.SessionDate.Date == targetDate)
            .ToListAsync(cancellationToken);

        var existingDict = existingList.ToDictionary(a => a.StudentId);
        int savedCount = 0;

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
                existing.RecordedByInstructorId = recordedByInstructorId;
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
                    RecordedByInstructorId = recordedByInstructorId
                };
                await _context.Attendances.AddAsync(newAtt, cancellationToken);
            }
            savedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.ReceiveAttendanceUpdate(new
        {
            courseId = request.CourseId,
            count = savedCount,
            date = targetDate
        });

        return savedCount;
    }

    public async Task<CourseAttendanceStatsDto?> GetCourseAttendanceStatsAsync(int courseId, CancellationToken cancellationToken = default)
    {
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

        return new CourseAttendanceStatsDto
        {
            CourseId = courseId,
            SessionsCount = sessionsCount,
            TotalRecords = totalRecords,
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            AttendanceRate = attendanceRate
        };
    }

    public async Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(int studentId, int courseId, CancellationToken cancellationToken = default)
    {
        var attendances = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.CourseId == courseId)
            .OrderByDescending(a => a.SessionDate)
            .ToListAsync(cancellationToken);

        int presentCount = attendances.Count(a => a.Status == AttendanceStatus.Present);
        int lateCount = attendances.Count(a => a.Status == AttendanceStatus.Late);
        int absentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        int totalSessions = attendances.Count;
        decimal attendanceRate = totalSessions > 0
            ? Math.Round(((decimal)(presentCount + lateCount) / totalSessions) * 100m, 1)
            : 0m;

        return new StudentAttendanceSummaryDto
        {
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            TotalSessions = totalSessions,
            AttendanceRate = attendanceRate,
            Sessions = attendances.Select(a => new AttendanceSessionDto
            {
                Id = a.Id,
                SessionDate = a.SessionDate,
                Status = a.Status.ToString(),
                Remarks = a.Remarks
            }).ToList()
        };
    }
}
