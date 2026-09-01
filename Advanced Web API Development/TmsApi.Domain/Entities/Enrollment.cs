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
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending; // Enrolled (Active/Completed), Applied (Pending), Waitlisted
    public string Batch { get; set; } = "Batch 1";
    public decimal Fee { get; set; } = 49m;
    public string PaymentStatus { get; set; } = "Paid"; // Paid, Pending, Unpaid
    public string PhoneNumber { get; set; } = "+1 202-555-0142";
    public string Country { get; set; } = "USA";
    public bool IsArchived { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
}
