namespace TmsApi.Application.DTOs.GradeDtos;

public class GradeDto
{
    public int EnrollmentId { get; set; }
    public decimal Grade { get; set; }
}

public class BulkGradeDto
{
    public List<GradeDto> Grades { get; set; } = new();
}

public class GradeResultDto
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal Grade { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public decimal UpdatedGpa { get; set; }
}

public class BulkGradebookItemDto
{
    public int StudentId { get; set; }
    public int AssessmentId { get; set; }
    public decimal ScoredPoints { get; set; }
    public string? Feedback { get; set; }
}

public class BulkGradebookRequestDto
{
    public int CourseId { get; set; }
    public List<BulkGradebookItemDto> Items { get; set; } = new();
}

public class CourseGradebookDto
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int ExpectedAssignments { get; set; }
    public bool HasMidterm { get; set; }
    public bool HasFinal { get; set; }
    public decimal TotalPossiblePoints { get; set; }
    public List<object> Columns { get; set; } = new();
    public List<object> Students { get; set; } = new();
}

public class StudentAcademicRecordDto
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalCreditsCompleted { get; set; }
    public decimal CumulativeGpa { get; set; }
    public string AcademicStanding { get; set; } = string.Empty;
    public List<object> Courses { get; set; } = new();
}

public class AssignGradeRequestDto
{
    public int EnrollmentId { get; set; }
    public string? LetterGrade { get; set; }
    public decimal? NumericScore { get; set; }
    public decimal? Grade { get; set; }
}

public class BulkGradeInputItemDto
{
    public int EnrollmentId { get; set; }
    public string? LetterGrade { get; set; }
    public decimal? NumericScore { get; set; }
    public decimal? Grade { get; set; }
}

public class BulkAssignGradesRequestDto
{
    public List<BulkGradeInputItemDto> Grades { get; set; } = new();
}

public class GradebookCellRequestDto
{
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
}

public class BulkGradebookItemInputDto
{
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
}

public class BulkGradebookSubmitRequestDto
{
    public int CourseId { get; set; }
    public List<BulkGradebookItemInputDto> Grades { get; set; } = new();
}
