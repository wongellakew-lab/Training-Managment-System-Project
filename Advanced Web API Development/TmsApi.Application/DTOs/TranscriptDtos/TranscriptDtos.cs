namespace TmsApi.Application.DTOs.TranscriptDtos;

public class TranscriptDto
{
    public string InstitutionName { get; set; } = "Training Management System";
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public TranscriptStudentDto Student { get; set; } = new();
    public TranscriptSummaryDto Summary { get; set; } = new();
    public List<TranscriptCourseRecordDto> CourseRecords { get; set; } = new();
}

public class TranscriptStudentDto
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Department { get; set; } = "General Academic Program";
    public DateTime EnrollmentDate { get; set; }
}

public class TranscriptSummaryDto
{
    public int TotalCoursesCompleted { get; set; }
    public int TotalCoursesAttempted { get; set; }
    public int TotalCreditsEarned { get; set; }
    public decimal CumulativeGpa { get; set; }
    public string AcademicStanding { get; set; } = "Good Standing";
}

public class TranscriptCourseRecordDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public int Credits { get; set; } = 3;
    public int DurationHours { get; set; } = 40;
    public string Term { get; set; } = "Term 1";
    public decimal? FinalScore { get; set; }
    public string GradeLetter { get; set; } = "P";
    public decimal GradePoint { get; set; } = 0.0m;
    public string Status { get; set; } = "Completed";
    public DateTime CompletionDate { get; set; }
}
