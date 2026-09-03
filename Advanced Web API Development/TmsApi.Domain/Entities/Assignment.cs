namespace TmsApi.Domain.Entities;

public class Assignment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? CourseModuleId { get; set; }
    public CourseModule? CourseModule { get; set; }

    public int? CourseLessonId { get; set; }
    public CourseLesson? CourseLesson { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public decimal MaxPoints { get; set; } = 100m;
    public DateTime DueDate { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedByInstructorId { get; set; }
    public Instructor? CreatedByInstructor { get; set; }
    public bool IsDeleted { get; set; } = false;

    public ICollection<Assessment> Submissions { get; set; } = new List<Assessment>();
}
