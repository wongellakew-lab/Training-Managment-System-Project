namespace TmsApi.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public string Category { get; set; } = "Design"; // Design, Web Dev, Marketing, Language, Communication, Business, Wellness
    public string Level { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced
    public string Status { get; set; } = "Active"; // Active, Draft, Archived
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
    public int MaxCapacity { get; set; } = 30;
    public string Prerequisites { get; set; } = "None";
    public DateTime? EnrollmentDeadline { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(42);
    public int DurationWeeks { get; set; } = 6;
    public bool IsDeleted { get; set; } = false;
    public string? RejectionReason { get; set; }

    public int ExpectedAssignmentCount { get; set; } = 5;
    public int ExpectedMidtermCount { get; set; } = 1;
    public int ExpectedFinalCount { get; set; } = 1;

    public int? InstructorId { get; set; }
    public Instructor? Instructor { get; set; }

    public ICollection<CourseSchedule> Schedules { get; set; } = new List<CourseSchedule>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<CourseModule> Modules { get; set; } = new List<CourseModule>();
    public ICollection<CourseResource> Resources { get; set; } = new List<CourseResource>();
}
