namespace TmsApi.Application.DTOs.GradeDtos;

public record AssignGradeInput(int EnrollmentId, string? LetterGrade, decimal? NumericScore, decimal? Grade);
public record BulkGradeInputItem(int EnrollmentId, string? LetterGrade, decimal? NumericScore, decimal? Grade);
public record BulkAssignInput(IEnumerable<BulkGradeInputItem> Grades);
public record GradebookCellRequest(int CourseId, int StudentId, int AssignmentId, decimal? Score, string? Feedback);
public record BulkGradebookItem(int StudentId, int AssignmentId, decimal? Score, string? Feedback);
public record BulkGradebookRequest(int CourseId, List<BulkGradebookItem> Grades);
