namespace TmsApi.Domain.Entities;

public class InstructorSuspensionHistory
{
    public int Id { get; set; }
    public int OriginalInstructorId { get; set; }
    public virtual Instructor? OriginalInstructor { get; set; }
    public int ReplacementInstructorId { get; set; }
    public virtual Instructor? ReplacementInstructor { get; set; }
    public int CourseId { get; set; }
    public virtual Course? Course { get; set; }
    public DateTime SuspendedAt { get; set; } = DateTime.UtcNow;
    public bool IsRestored { get; set; } = false;
    public DateTime? RestoredAt { get; set; }
    public string? Reason { get; set; }
}
