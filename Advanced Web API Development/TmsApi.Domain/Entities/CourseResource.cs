namespace TmsApi.Domain.Entities;

public class CourseResource
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string FileType { get; set; } = "PDF";
    public string FileSize { get; set; } = "1.0 MB";
    public string? Url { get; set; }
}
