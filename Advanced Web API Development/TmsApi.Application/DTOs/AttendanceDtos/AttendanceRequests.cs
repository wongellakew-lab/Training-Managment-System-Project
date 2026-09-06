namespace TmsApi.Application.DTOs.AttendanceDtos;

public record MarkAttendanceRequest(int StudentId, int CourseId, int? ScheduleId, DateTime SessionDate, int Status, string? Remarks);
public record BulkAttendanceItem(int StudentId, int Status, string? Remarks);
public record BulkAttendanceRequest(int CourseId, int? ScheduleId, DateTime SessionDate, List<BulkAttendanceItem> Items);
