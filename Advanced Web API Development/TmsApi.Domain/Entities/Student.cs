namespace TmsApi.Domain.Entities;

public class Student
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public decimal Gpa { get; set; } = 0.0m;
    public bool IsDeleted { get; set; } = false;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
