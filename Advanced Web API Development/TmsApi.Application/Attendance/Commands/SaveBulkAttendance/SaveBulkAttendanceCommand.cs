using MediatR;
using TmsApi.Application.DTOs.AttendanceDtos;

namespace TmsApi.Application.Attendance.Commands;

public record SaveBulkAttendanceCommand(
    BulkAttendanceRequestDto Request,
    int? RecordedByInstructorId) : IRequest<int>;
