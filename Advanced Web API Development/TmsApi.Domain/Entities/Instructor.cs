namespace TmsApi.Domain.Entities;

public class Instructor
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public string? UserId { get; set; }
    public virtual TmsUser? User { get; set; }

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
