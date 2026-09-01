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
