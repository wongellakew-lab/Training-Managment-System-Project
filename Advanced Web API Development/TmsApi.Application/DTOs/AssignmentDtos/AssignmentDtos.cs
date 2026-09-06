namespace TmsApi.Application.DTOs.AssignmentDtos;

public class AssignmentDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int AssignmentNumber { get; set; }
    public int? CourseModuleId { get; set; }
    public string? ModuleTitle { get; set; }
    public int? CourseLessonId { get; set; }
    public string? LessonTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public decimal MaxPoints { get; set; } = 100m;
    public DateTime DueDate { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public bool IsPublished { get; set; }
    public int TotalSubmissions { get; set; }
    public int SubmissionsCount { get; set; }
    public int PendingGrading { get; set; }
    public int GradedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAssignmentDto
{
    public int CourseId { get; set; }
    public int AssignmentNumber { get; set; }
    public int? CourseModuleId { get; set; }
    public int? CourseLessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public decimal MaxPoints { get; set; } = 100m;
    public DateTime DueDate { get; set; }
    public bool IsPublished { get; set; }
}

public class UpdateAssignmentDto
{
    public string Title { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public decimal MaxPoints { get; set; } = 100m;
    public DateTime DueDate { get; set; }
    public bool IsPublished { get; set; }
}

public class AssignmentSubmissionDto
{
    public int Id { get; set; }
    public int? AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public string? AssignmentInstructions { get; set; }
    public string? AssignmentAttachmentUrl { get; set; }
    public decimal MaxPoints { get; set; }
    public int? CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? StudentRegistrationNumber { get; set; }
    public string? StudentAvatar { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Submitted";
    public decimal? ScoredPoints { get; set; }
    public decimal MaxScore { get; set; }
    public string? Feedback { get; set; }
    public string? RubricScoresJson { get; set; }
    public DateTime? GradedAt { get; set; }
    public string? GradedBy { get; set; }
    public string? SubmissionText { get; set; }
    public string? SubmissionFileUrl { get; set; }
    public string? SubmissionFileName { get; set; }
    public string? SubmissionFileType { get; set; }
    public long? SubmissionFileSize { get; set; }
}

public class GradeSubmissionDto
{
    public decimal ScoredPoints { get; set; }
    public string? Feedback { get; set; }
    public string? RubricScoresJson { get; set; }
}

public class SubmitAssignmentDto
{
    public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
    public string? SubmissionText { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
}
