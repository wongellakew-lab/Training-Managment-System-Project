namespace TmsApi.Application.DTOs.ScheduleDtos;

public class ScheduleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Meetings"; // Meetings, Workshops, Events
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = "09:00 AM";
    public string EndTime { get; set; } = "10:00 AM";
    public string PicName { get; set; } = string.Empty;
    public string PicAvatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int AttendeesCount { get; set; } = 0;
    public int? CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string? CourseCode { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorAvatar { get; set; }
    public string ScheduleType { get; set; } = "Lecture";
    public string TargetAudience { get; set; } = "All"; // All, AllStudents, AllInstructors, CourseStudents, DepartmentInstructors
    public int? TargetCourseId { get; set; }
    public string? TargetDepartment { get; set; }
    public string? CourseStatus { get; set; }
    public string? EnrollmentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourseLookupDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class StudentScheduleSummaryDto
{
    public int ClassesThisMonth { get; set; }
    public int ClassesThisWeek { get; set; }
    public int EnrolledCoursesCount { get; set; }
    public ScheduleDto? NextClass { get; set; }
    public List<CourseLookupDto> EnrolledCourses { get; set; } = new();
    public List<ScheduleDto> Schedules { get; set; } = new();
}

public class CreateScheduleDto
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Meetings";
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = "09:00 AM";
    public string EndTime { get; set; } = "10:00 AM";
    public string PicName { get; set; } = string.Empty;
    public string PicAvatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int AttendeesCount { get; set; } = 0;
    public string TargetAudience { get; set; } = "All";
    public int? TargetCourseId { get; set; }
    public string? TargetDepartment { get; set; }
}

public class UpdateScheduleDto
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Meetings";
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = "09:00 AM";
    public string EndTime { get; set; } = "10:00 AM";
    public string PicName { get; set; } = string.Empty;
    public string PicAvatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int AttendeesCount { get; set; } = 0;
    public string TargetAudience { get; set; } = "All";
    public int? TargetCourseId { get; set; }
    public string? TargetDepartment { get; set; }
}
