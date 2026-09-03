namespace TmsApi.Domain.Entities;

public class Assessment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int? AssignmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal MaxScore { get; set; } = 100m;
    public decimal? ScoredPoints { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Submitted, Graded
    public string? Feedback { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime? GradedAt { get; set; }
    public string? RubricScoresJson { get; set; }
    public string? SubmissionFileUrl { get; set; }
    public string? SubmissionText { get; set; }

    public Student? Student { get; set; }
    public Course? Course { get; set; }
    public Assignment? Assignment { get; set; }
}
