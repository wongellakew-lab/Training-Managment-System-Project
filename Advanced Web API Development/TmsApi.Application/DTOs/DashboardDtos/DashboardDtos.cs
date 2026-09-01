using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.DTOs.ScheduleDtos;

namespace TmsApi.Application.DTOs.DashboardDtos;

public class StudentDashboardDto
{
    public decimal CumulativeGpa { get; set; }
    public int ActiveEnrollmentsCount { get; set; }
    public int CompletedCoursesCount { get; set; }
    public int EarnedCredits { get; set; }
    public int CertificatesCount { get; set; }
    public int TotalEnrollmentsCount { get; set; }
    public IEnumerable<EnrollmentDto> RecentEnrollments { get; set; } = new List<EnrollmentDto>();
    public IEnumerable<ScheduleDto> UpcomingSchedules { get; set; } = new List<ScheduleDto>();
}

public class InstructorDashboardDto
{
    public int AssignedCoursesCount { get; set; }
    public int ActiveStudentsCount { get; set; }
    public int PendingEnrollmentsCount { get; set; }
    public int PendingGradesCount { get; set; }
    public int CertificatesAwardedCount { get; set; }
    public IEnumerable<EnrollmentDto> RecentEnrollments { get; set; } = new List<EnrollmentDto>();
    public IEnumerable<CourseDto> AssignedCourses { get; set; } = new List<CourseDto>();
}

public class AdminDashboardDto
{
    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalCourses { get; set; }
    public int ActiveEnrollments { get; set; }
    public int PendingEnrollments { get; set; }
    public int WaitlistedEnrollments { get; set; }
    public int TotalCertificates { get; set; }

    // Revenue Widget (computed from enrollment fees)
    public decimal TotalRevenue { get; set; }
    public List<MonthlyRevenueDto> RevenueHistory { get; set; } = new();

    // Enrollment Trends Widget
    public decimal TotalEnrollmentValue { get; set; }
    public decimal EnrollmentGrowthPercentage { get; set; }
    public List<MonthlyEnrollmentTrendDto> EnrollmentTrends { get; set; } = new();

    // Top Courses Donut Widget
    public List<TopCourseStatDto> TopCoursesDistribution { get; set; } = new();

    // Learning Activity 7x6 Heatmap
    public List<ActivityHeatmapCellDto> LearningActivityHeatmap { get; set; } = new();

    // Student Demographics Widget
    public List<CountryDemographicDto> StudentDemographics { get; set; } = new();

    // New Courses Widget
    public List<CourseDto> NewCourses { get; set; } = new();

    // Right Sidebar Mini Calendar Schedules
    public List<ScheduleDto> UpcomingSchedules { get; set; } = new();

    // Recent Activities Feed (from notifications)
    public List<SystemActivityDto> RecentActivities { get; set; } = new();

    public IEnumerable<EnrollmentDto> PendingApprovalList { get; set; } = new List<EnrollmentDto>();
    public IEnumerable<SystemActivityDto> RecentActivity { get; set; } = new List<SystemActivityDto>();
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
    public decimal Expense { get; set; }
}

public class MonthlyEnrollmentTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsPeak { get; set; }
}

public class TopCourseStatDto
{
    public string Name { get; set; } = string.Empty;
    public int StudentsCount { get; set; }
    public int Percentage { get; set; }
    public string Color { get; set; } = "#1E40AF";
}

public class ActivityHeatmapCellDto
{
    public string Day { get; set; } = "Mon";
    public string TimeSlot { get; set; } = "9am";
    public int Level { get; set; } = 1;
    public int Value { get; set; }
}

public class CountryDemographicDto
{
    public string Country { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public int StudentsCount { get; set; }
    public int Percentage { get; set; }
}

public class PaymentStatusBreakdownDto
{
    public decimal TotalPaid { get; set; }
    public int PaidStudentsCount { get; set; }
    public decimal PaidPercentage { get; set; }

    public decimal TotalPending { get; set; }
    public int PendingStudentsCount { get; set; }
    public decimal PendingPercentage { get; set; }

    public decimal TotalUnpaid { get; set; }
    public int UnpaidStudentsCount { get; set; }
    public decimal UnpaidPercentage { get; set; }

    public int EnrolledPaid { get; set; }
    public int EnrolledPending { get; set; }
    public int EnrolledUnpaid { get; set; }

    public int AppliedPaid { get; set; }
    public int AppliedPending { get; set; }
    public int AppliedUnpaid { get; set; }

    public int WaitlistedPaid { get; set; }
    public int WaitlistedPending { get; set; }
    public int WaitlistedUnpaid { get; set; }
}

public class SystemActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "System";
    public string UserAvatar { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
