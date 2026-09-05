using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.DTOs.CourseDtos;

public class StudentEnrolledCourseDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced
    public string Department { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Credits { get; set; } = 3;
    public int DurationWeeks { get; set; } = 6;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Schedule Information
    public List<CourseScheduleDto> Schedules { get; set; } = new();
    public string DaysDisplay { get; set; } = string.Empty; // e.g. "Mon • Wed • Fri"
    public string TimeDisplay { get; set; } = string.Empty; // e.g. "3:00 PM – 4:00 PM"
    public string Location { get; set; } = "Main Training Center";
    public string Room { get; set; } = "Room 204";

    // Instructor Information
    public int? InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string? InstructorAvatar { get; set; }
    public string? InstructorDepartment { get; set; }

    // Enrollment & Progress Information
    public int EnrollmentId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string EnrollmentStatus { get; set; } = "Active";
    public string AcademicStatus { get; set; } = "Active"; // "Active", "Upcoming", "Completed", "Withdrawn"
    public decimal ProgressPercentage { get; set; }
    public int CompletedAssessments { get; set; }
    public int TotalAssessments { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalModules { get; set; }
    public int CompletedModules { get; set; }
    public decimal? Grade { get; set; }
}

public class StudentCoursesStatisticsDto
{
    public int TotalEnrolledCourses { get; set; }
    public int ActiveCourses { get; set; }
    public int CompletedCourses { get; set; }
    public int UpcomingCourses { get; set; }
    public int TotalCredits { get; set; }
    public decimal OverallProgressPercentage { get; set; }
    public int CurrentSemesterCourses { get; set; }
    public int UpcomingClassesCount { get; set; }
}
