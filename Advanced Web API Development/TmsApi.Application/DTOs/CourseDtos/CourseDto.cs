namespace TmsApi.Application.DTOs.CourseDtos;

public class CourseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public string? InstructorId { get; set; }
    public int EnrolledCount { get; set; }
    public bool IsFull => EnrolledCount >= MaxCapacity;
}

public class CreateCourseDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public string? InstructorId { get; set; }
}

public class UpdateCourseDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public string? InstructorId { get; set; }
}
