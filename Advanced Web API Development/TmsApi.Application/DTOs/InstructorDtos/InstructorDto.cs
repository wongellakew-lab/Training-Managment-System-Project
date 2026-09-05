namespace TmsApi.Application.DTOs.InstructorDtos;

public class InstructorDto
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public string Status { get; set; } = "Active";
    public int CoursesCount { get; set; }
    public int StudentsCount { get; set; }
    public decimal Rating { get; set; } = 4.8m;
    public string Avatar { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public List<string> CourseTitles { get; set; } = new();
}

public class InstructorCourseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int EnrolledStudentsCount { get; set; }
    public int LessonsCount { get; set; }
    public decimal Rating { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class InstructorDetailDto : InstructorDto
{
    public string? Bio { get; set; }
    public List<InstructorCourseDto> Courses { get; set; } = new();
}

public class InstructorStatsDto
{
    public int TotalInstructors { get; set; }
    public int ActiveMentors { get; set; }
    public decimal AverageRating { get; set; }
}

public class CreateInstructorDto
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class CourseReassignmentItemDto
{
    public int CourseId { get; set; }
    public int ReplacementInstructorId { get; set; }
}

public class SuspendInstructorRequestDto
{
    public List<CourseReassignmentItemDto> CourseReassignments { get; set; } = new();
    public string? Reason { get; set; }
}
