using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Attendance.Commands;

public sealed class MarkAttendanceCommandHandler(IAttendanceService _attendanceService)
    : IRequestHandler<MarkAttendanceCommand, bool>
{
    public async Task<bool> Handle(MarkAttendanceCommand request, CancellationToken cancellationToken)
    {
        return await _attendanceService.MarkAttendanceAsync(
            request.StudentId,
            request.CourseId,
            request.ScheduleId,
            request.SessionDate,
            request.Status,
            request.Remarks,
            request.RecordedByInstructorId,
            cancellationToken);
    }
}
