using System.Text.Json.Serialization;

namespace TmsApi.Application.DTOs.AttendanceDtos;

public class StudentAttendanceItemDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "Unmarked";
    public int StatusCode { get; set; }
    public string? Remarks { get; set; }
    public DateTime? RecordedAt { get; set; }
}

public class ScheduleInfoDto
{
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = string.Empty;
}

public class CourseAttendanceSessionDto
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    [JsonPropertyName("hasScheduledSession")]
    public bool HasScheduledSession { get; set; }
    [JsonPropertyName("isRecorded")]
    public bool IsRecorded { get; set; }
    public ScheduleInfoDto? ScheduleInfo { get; set; }
    public int TotalEnrolled { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public List<StudentAttendanceItemDto> Students { get; set; } = new();
}

public class BulkAttendanceItemDto
{
    public int StudentId { get; set; }
    public int Status { get; set; }
    public string? Remarks { get; set; }
}

public class BulkAttendanceRequestDto
{
    public int CourseId { get; set; }
    public int? ScheduleId { get; set; }
    public DateTime SessionDate { get; set; }
    public List<BulkAttendanceItemDto> Items { get; set; } = new();
}

public class CourseAttendanceStatsDto
{
    public int CourseId { get; set; }
    public int SessionsCount { get; set; }
    public int TotalRecords { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class AttendanceSessionDto
{
    public int Id { get; set; }
    public DateTime SessionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class StudentAttendanceSummaryDto
{
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int TotalSessions { get; set; }
    public decimal AttendanceRate { get; set; }
    public List<AttendanceSessionDto> Sessions { get; set; } = new();
}
