namespace TmsApi.Domain.Entities;

public class CourseLesson
{
    public int Id { get; set; }
    public int CourseModuleId { get; set; }
    public CourseModule CourseModule { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; } = 1;
    public int DurationMinutes { get; set; } = 45;
    public string? ContentText { get; set; }
    public string? PdfUrl { get; set; }
    public string? PdfFileName { get; set; }
}
