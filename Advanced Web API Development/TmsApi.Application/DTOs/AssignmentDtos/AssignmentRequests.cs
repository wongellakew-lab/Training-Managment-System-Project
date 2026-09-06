namespace TmsApi.Application.DTOs.AssignmentDtos;

public record CreateAssignmentRequest(
    int CourseId,
    int AssignmentNumber,
    int? CourseModuleId,
    int? CourseLessonId,
    string Title,
    string Instructions,
    decimal MaxPoints,
    DateTime DueDate,
    bool IsPublished);

public record UpdateAssignmentRequest(
    string Title,
    string Instructions,
    decimal MaxPoints,
    DateTime DueDate,
    bool IsPublished);

public record GradeSubmissionRequest(
    decimal ScoredPoints,
    string? Feedback,
    string? RubricScoresJson);
