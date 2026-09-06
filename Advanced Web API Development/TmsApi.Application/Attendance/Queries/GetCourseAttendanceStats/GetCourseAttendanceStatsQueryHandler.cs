using MediatR;
using TmsApi.Application.DTOs.AttendanceDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Attendance.Queries;

public sealed class GetCourseAttendanceStatsQueryHandler(IAttendanceService _attendanceService)
    : IRequestHandler<GetCourseAttendanceStatsQuery, CourseAttendanceStatsDto>
{
    public async Task<CourseAttendanceStatsDto> Handle(GetCourseAttendanceStatsQuery request, CancellationToken cancellationToken)
    {
        return await _attendanceService.GetCourseAttendanceStatsAsync(request.CourseId, cancellationToken);
    }
}
