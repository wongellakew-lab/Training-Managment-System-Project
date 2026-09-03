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
    public int CoursesCount { get; set; }
    public int StudentsCount { get; set; }
    public decimal Rating { get; set; } = 4.8m;
    public string Avatar { get; set; } = string.Empty;
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
