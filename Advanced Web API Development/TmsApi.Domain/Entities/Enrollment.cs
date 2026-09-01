namespace TmsApi.Domain.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public decimal? Grade { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public bool IsArchived { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
}
