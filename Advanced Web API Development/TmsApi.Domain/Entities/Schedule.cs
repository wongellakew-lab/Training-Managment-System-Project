namespace TmsApi.Domain.Entities;

public class Schedule
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
    public string Notes { get; set; } = string.Empty;
    public int AttendeesCount { get; set; } = 0;
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
    public string ScheduleType { get; set; } = "Lecture"; // Lecture, Lab, Workshop, Exam
    public string TargetAudience { get; set; } = "All"; // All, AllStudents, AllInstructors, CourseStudents, DepartmentInstructors
    public int? TargetCourseId { get; set; }
    public Course? TargetCourse { get; set; }
    public string? TargetDepartment { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
