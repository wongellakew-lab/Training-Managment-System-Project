using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Attendance.Commands;

public sealed class SaveBulkAttendanceCommandHandler(IAttendanceService _attendanceService)
    : IRequestHandler<SaveBulkAttendanceCommand, int>
{
    public async Task<int> Handle(SaveBulkAttendanceCommand request, CancellationToken cancellationToken)
    {
        return await _attendanceService.SaveBulkAttendanceAsync(
            request.Request,
            request.RecordedByInstructorId,
            cancellationToken);
    }
}
