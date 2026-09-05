using System.Text.Json.Serialization;

namespace TmsApi.Application.DTOs.CourseDtos;

public class CourseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public string Category { get; set; } = "Design";
    public string Level { get; set; } = "Beginner";
    public string Status { get; set; } = "Active";
    public int DurationHours { get; set; } = 40;
    public int LessonsCount { get; set; } = 20;
    public decimal Rating { get; set; } = 4.9m;
    public int ReviewsCount { get; set; } = 320;
    public string ImageUrl { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public string FeaturesJson { get; set; } = "[]";
    public string ResourcesJson { get; set; } = "[]";
    public int EnrolledStudentsCount { get; set; } = 320;
    public int Credits { get; set; } = 3;
    public int MaxCapacity { get; set; } = 50;
    public string Prerequisites { get; set; } = "None";
    public DateTime? EnrollmentDeadline { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationWeeks { get; set; } = 6;
    public int? InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorAvatar { get; set; }
    public string? InstructorDepartment { get; set; }
    public string? InstructorEmail { get; set; }
    public int EnrolledCount { get; set; }
    public int AvailableSeats => Math.Max(0, MaxCapacity - EnrolledCount);
    public bool IsFull => EnrolledCount >= MaxCapacity;
    public bool IsActive => Status == "Active" && !IsFull;
    public bool IsStudentEnrolled { get; set; } = false;
    public string? StudentEnrollmentStatus { get; set; }
    public decimal? StudentProgress { get; set; }
    public string? RejectionReason { get; set; }
    public int ExpectedAssignmentCount { get; set; } = 5;
    public int ExpectedMidtermCount { get; set; } = 1;
    public int ExpectedFinalCount { get; set; } = 1;
    public int TotalAssessments => ExpectedAssignmentCount + ExpectedMidtermCount + ExpectedFinalCount;
    public int CreatedAssignmentsCount { get; set; }
    public List<CourseScheduleDto> Schedules { get; set; } = new();
    public List<CourseModuleDto> Modules { get; set; } = new();
    public List<CourseResourceDto> Resources { get; set; } = new();
    public IEnumerable<TmsApi.Application.DTOs.EnrollmentDtos.EnrollmentDto>? Enrollments { get; set; }
}

public class CourseModuleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; } = 1;
    public List<CourseLessonDto> Lessons { get; set; } = new();
}

public class CourseLessonDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; } = 1;
    public int DurationMinutes { get; set; } = 45;
    public string? ContentText { get; set; }
    public string? PdfUrl { get; set; }
    public string? PdfFileName { get; set; }
}

public class CourseResourceDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileType { get; set; } = "PDF";
    public string FileSize { get; set; } = "1.0 MB";
    public string? Url { get; set; }
}

public class CourseScheduleDto
{
    public int Id { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek DayOfWeek { get; set; }
    public string DayName => DayOfWeek.ToString();
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = "Main Training Center";
    public string Room { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "Lecture";
}

public class CreateCourseScheduleDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = "09:00 AM";
    public string EndTime { get; set; } = "10:30 AM";
    public string Location { get; set; } = "Main Training Center";
    public string Room { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "Lecture";
}

public class CreateCourseDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public string Category { get; set; } = "Design";
    public string Level { get; set; } = "Beginner";
    public string Status { get; set; } = "Active";
    public int DurationHours { get; set; } = 40;
    public int LessonsCount { get; set; } = 20;
    public string ImageUrl { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public string FeaturesJson { get; set; } = "[]";
    public string ResourcesJson { get; set; } = "[]";
    public int Credits { get; set; } = 3;
    public int MaxCapacity { get; set; } = 50;
    public int? InstructorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int DurationWeeks { get; set; } = 6;
    public int ExpectedAssignmentCount { get; set; } = 5;
    public int ExpectedMidtermCount { get; set; } = 1;
    public int ExpectedFinalCount { get; set; } = 1;
    public List<CreateCourseScheduleDto> Schedules { get; set; } = new();
    public List<CourseModuleDto> Modules { get; set; } = new();
}

public class UpdateCourseDto
{
    public string? Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public string Category { get; set; } = "Design";
    public string Level { get; set; } = "Beginner";
    public string Status { get; set; } = "Active";
    public int DurationHours { get; set; } = 40;
    public int LessonsCount { get; set; } = 20;
    public decimal Rating { get; set; } = 4.9m;
    public int ReviewsCount { get; set; } = 320;
    public string ImageUrl { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public string FeaturesJson { get; set; } = "[]";
    public string ResourcesJson { get; set; } = "[]";
    public int Credits { get; set; } = 3;
    public int MaxCapacity { get; set; } = 50;
    public int? InstructorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int DurationWeeks { get; set; } = 6;
    public int ExpectedAssignmentCount { get; set; } = 5;
    public int ExpectedMidtermCount { get; set; } = 1;
    public int ExpectedFinalCount { get; set; } = 1;
    public List<CreateCourseScheduleDto> Schedules { get; set; } = new();
    public List<CourseModuleDto> Modules { get; set; } = new();
}

public class RejectCourseDto
{
    public string Reason { get; set; } = string.Empty;
}
