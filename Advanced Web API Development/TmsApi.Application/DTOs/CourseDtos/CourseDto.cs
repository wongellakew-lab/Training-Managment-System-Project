namespace TmsApi.Application.DTOs.CourseDtos;

public class CourseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public int Credits { get; set; } = 3;
    public int MaxCapacity { get; set; }
    public int? InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public int EnrolledCount { get; set; }
    public bool IsFull => EnrolledCount >= MaxCapacity;
    public bool IsActive => !IsFull;
}

public class CreateCourseDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public int Credits { get; set; } = 3;
    public int MaxCapacity { get; set; } = 30;
    public int? InstructorId { get; set; }
}

public class UpdateCourseDto
{
    public string? Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = "Computer Science";
    public int Credits { get; set; } = 3;
    public int MaxCapacity { get; set; }
    public int? InstructorId { get; set; }
}
