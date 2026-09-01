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
    public decimal Price { get; set; } = 89m;
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
    public int? InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorAvatar { get; set; }
    public int EnrolledCount { get; set; }
    public bool IsFull => EnrolledCount >= MaxCapacity;
    public bool IsActive => Status == "Active" && !IsFull;
}

public class CreateCourseDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public string Category { get; set; } = "Design";
    public string Level { get; set; } = "Beginner";
    public decimal Price { get; set; } = 89m;
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
}

public class UpdateCourseDto
{
    public string? Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public string Category { get; set; } = "Design";
    public string Level { get; set; } = "Beginner";
    public decimal Price { get; set; } = 89m;
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
    public int MaxCapacity { get; set; }
    public int? InstructorId { get; set; }
}
