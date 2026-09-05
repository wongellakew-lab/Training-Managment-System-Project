namespace TmsApi.Application.DTOs.StudentDtos;

public class StudentDto
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public decimal Gpa { get; set; }
    public int CoursesEnrolled { get; set; }
    public string Status { get; set; } = "Active";
    public string Avatar { get; set; } = string.Empty;
}

public class StudentStatsDto
{
    public int TotalStudents { get; set; }
    public int ActiveEnrolled { get; set; }
    public int CertificatesIssued { get; set; }
}

public class CreateStudentDto
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateStudentDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AdminStudentEnrolledCourseDto
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DurationHours { get; set; }
    public int LessonsCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string EnrollmentStatus { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public decimal ProgressPercentage { get; set; }
    public decimal? FinalScore { get; set; }
    public string? InstructorName { get; set; }
}

public class AdminStudentDetailDto
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Bio { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public decimal Gpa { get; set; }
    public string Status { get; set; } = "Active";
    public string Avatar { get; set; } = string.Empty;
    public List<AdminStudentEnrolledCourseDto> EnrolledCourses { get; set; } = new();
}
