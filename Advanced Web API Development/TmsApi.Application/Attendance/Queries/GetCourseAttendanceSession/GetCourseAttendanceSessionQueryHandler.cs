using MediatR;
using TmsApi.Application.DTOs.AttendanceDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Attendance.Queries;

public sealed class GetCourseAttendanceSessionQueryHandler(IAttendanceService _attendanceService)
    : IRequestHandler<GetCourseAttendanceSessionQuery, CourseAttendanceSessionDto?>
{
    public async Task<CourseAttendanceSessionDto?> Handle(GetCourseAttendanceSessionQuery request, CancellationToken cancellationToken)
    {
        return await _attendanceService.GetCourseAttendanceSessionAsync(request.CourseId, request.Date, cancellationToken);
    }
}
