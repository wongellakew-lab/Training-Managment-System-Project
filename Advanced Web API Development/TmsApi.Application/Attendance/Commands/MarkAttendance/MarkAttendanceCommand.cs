using MediatR;

namespace TmsApi.Application.Attendance.Commands;

public record MarkAttendanceCommand(
    int StudentId,
    int CourseId,
    int? ScheduleId,
    DateTime SessionDate,
    int Status,
    string? Remarks,
    int? RecordedByInstructorId) : IRequest<bool>;
