using TmsApi.Application.DTOs.EnrollmentDtos;

namespace TmsApi.Application.DTOs.DashboardDtos;

public class StudentDashboardDto
{
    public decimal CumulativeGpa { get; set; }
    public int ActiveEnrollmentsCount { get; set; }
    public int EarnedCredits { get; set; }
    public int CertificatesCount { get; set; }
    public IEnumerable<EnrollmentDto> RecentEnrollments { get; set; } = new List<EnrollmentDto>();
}

public class InstructorDashboardDto
{
    public int AssignedCoursesCount { get; set; }
    public int ActiveStudentsCount { get; set; }
    public int PendingGradesCount { get; set; }
    public int CertificatesAwardedCount { get; set; }
    public IEnumerable<EnrollmentDto> RecentEnrollments { get; set; } = new List<EnrollmentDto>();
}

public class AdminDashboardDto
{
    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalCourses { get; set; }
    public int ActiveEnrollments { get; set; }
    public int PendingEnrollments { get; set; }
    public int TotalCertificates { get; set; }
    public IEnumerable<EnrollmentDto> PendingApprovalList { get; set; } = new List<EnrollmentDto>();
    public IEnumerable<SystemActivityDto> RecentActivity { get; set; } = new List<SystemActivityDto>();
}

public class SystemActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "System";
    public DateTime Timestamp { get; set; }
}
