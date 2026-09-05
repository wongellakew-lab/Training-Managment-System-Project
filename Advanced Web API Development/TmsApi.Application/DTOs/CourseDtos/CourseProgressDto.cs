namespace TmsApi.Application.DTOs.CourseDtos;

public class CourseProgressDto
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public decimal AssignmentProgress { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalModules { get; set; }
    public int CompletedModules { get; set; }
    public decimal OverallProgress { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? CurrentGrade { get; set; }
}
