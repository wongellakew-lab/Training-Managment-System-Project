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
