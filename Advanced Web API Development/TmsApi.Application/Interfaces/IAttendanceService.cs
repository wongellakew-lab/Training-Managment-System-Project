using TmsApi.Application.DTOs.AttendanceDtos;

namespace TmsApi.Application.Interfaces;

public interface IAttendanceService
{
    Task<CourseAttendanceSessionDto?> GetCourseAttendanceSessionAsync(int courseId, DateTime? date, CancellationToken cancellationToken = default);
    Task<bool> MarkAttendanceAsync(int studentId, int courseId, int? scheduleId, DateTime sessionDate, int status, string? remarks, int? recordedByInstructorId, CancellationToken cancellationToken = default);
    Task<int> SaveBulkAttendanceAsync(BulkAttendanceRequestDto request, int? recordedByInstructorId, CancellationToken cancellationToken = default);
    Task<CourseAttendanceStatsDto?> GetCourseAttendanceStatsAsync(int courseId, CancellationToken cancellationToken = default);
    Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(int studentId, int courseId, CancellationToken cancellationToken = default);
}
