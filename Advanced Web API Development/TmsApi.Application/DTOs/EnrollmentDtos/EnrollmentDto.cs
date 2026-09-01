using TmsApi.Domain.Entities;

namespace TmsApi.Application.DTOs.EnrollmentDtos;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public decimal? Grade { get; set; }
    public DateTime EnrolledAt { get; set; }
    public EnrollmentStatus Status { get; set; }
    public bool IsArchived { get; set; }
}

public class CreateEnrollmentDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
}

public class UpdateEnrollmentStatusDto
{
    public EnrollmentStatus Status { get; set; }
}
