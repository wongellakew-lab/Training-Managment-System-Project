using System.Text.Json.Serialization;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.DTOs.EnrollmentDtos;

public class EnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentAvatar { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCategory { get; set; } = "Design";
    public string Batch { get; set; } = "Batch 1";
    public string Country { get; set; } = "USA";
    public decimal? Grade { get; set; }
    public DateTime EnrolledAt { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnrollmentStatus Status { get; set; }
    public bool IsArchived { get; set; }
}

public class CreateEnrollmentDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Batch { get; set; } = "Batch 1";
    public string PhoneNumber { get; set; } = "+1 202-555-0142";
    public string Country { get; set; } = "USA";
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}

public class UpdateEnrollmentStatusDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnrollmentStatus Status { get; set; }
}
