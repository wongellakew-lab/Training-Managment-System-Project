using MediatR;
using TmsApi.Application.DTOs.AttendanceDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Attendance.Queries;

public sealed class GetStudentAttendanceSummaryQueryHandler(IAttendanceService _attendanceService)
    : IRequestHandler<GetStudentAttendanceSummaryQuery, StudentAttendanceSummaryDto?>
{
    public async Task<StudentAttendanceSummaryDto?> Handle(GetStudentAttendanceSummaryQuery request, CancellationToken cancellationToken)
    {
        return await _attendanceService.GetStudentAttendanceSummaryAsync(request.StudentId, request.CourseId, cancellationToken);
    }
}
