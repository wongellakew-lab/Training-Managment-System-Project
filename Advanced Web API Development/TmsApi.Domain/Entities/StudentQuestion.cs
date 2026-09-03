namespace TmsApi.Domain.Entities;

public class StudentQuestion
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Question { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public bool IsAnswered { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
    public int? AnsweredByInstructorId { get; set; }
    public Instructor? AnsweredByInstructor { get; set; }
}
