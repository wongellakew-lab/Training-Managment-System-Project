namespace TmsApi.Domain.Entities;

public class Assessment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal MaxScore { get; set; } = 100m;
    public decimal? ScoredPoints { get; set; }
    public DateTime DueDate { get; set; }
}
