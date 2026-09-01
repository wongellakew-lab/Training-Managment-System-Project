using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.DTOs.ScheduleDtos;

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
    public int TotalStudents { get; set; } = 18500;
    public int TotalInstructors { get; set; } = 85;
    public int TotalCourses { get; set; } = 320;
    public int ActiveEnrollments { get; set; } = 14280;
    public int PendingEnrollments { get; set; } = 3120;
    public int WaitlistedEnrollments { get; set; } = 1140;
    public int TotalCertificates { get; set; } = 5420;

    // Revenue Widget
    public decimal TotalRevenue { get; set; } = 294500m;
    public List<MonthlyRevenueDto> RevenueHistory { get; set; } = new();

    // Enrollment Trends Widget
    public decimal TotalEnrollmentValue { get; set; } = 12480m;
    public decimal EnrollmentGrowthPercentage { get; set; } = 12.5m;
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

    // Recent Activities Feed
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
    public string Day { get; set; } = "Mon"; // Mon, Tue, Wed, Thu, Fri, Sat, Sun
    public string TimeSlot { get; set; } = "9am"; // 7am, 9am, 11am, 1pm, 3pm, 6pm
    public int Level { get; set; } = 1; // 0=Very Low, 1=Low, 2=Moderate, 3=High, 4=Very High
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
    public decimal TotalPaid { get; set; } = 946210m;
    public int PaidStudentsCount { get; set; } = 13180;
    public decimal PaidPercentage { get; set; } = 70.3m;

    public decimal TotalPending { get; set; } = 268340m;
    public int PendingStudentsCount { get; set; } = 3900;
    public decimal PendingPercentage { get; set; } = 20.8m;

    public decimal TotalUnpaid { get; set; } = 96179m;
    public int UnpaidStudentsCount { get; set; } = 1660;
    public decimal UnpaidPercentage { get; set; } = 8.9m;

    public int EnrolledPaid { get; set; } = 12800;
    public int EnrolledPending { get; set; } = 1200;
    public int EnrolledUnpaid { get; set; } = 280;

    public int AppliedPaid { get; set; } = 200;
    public int AppliedPending { get; set; } = 2400;
    public int AppliedUnpaid { get; set; } = 520;

    public int WaitlistedPaid { get; set; } = 180;
    public int WaitlistedPending { get; set; } = 300;
    public int WaitlistedUnpaid { get; set; } = 660;
}

public class SystemActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "System"; // Profile Updated, Course Completed, User Feedback Submitted
    public string UserAvatar { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
