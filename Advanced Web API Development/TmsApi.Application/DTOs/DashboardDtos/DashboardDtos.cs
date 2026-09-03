using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.DTOs.ScheduleDtos;

namespace TmsApi.Application.DTOs.DashboardDtos;

public class StudentUpcomingClassDto
{
    public int ScheduleId { get; set; }
    public int? CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "Lecture";
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class StudentPendingAssignmentDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Pending";
}

public class StudentRecentGradeDto
{
    public int AssessmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string AssessmentTitle { get; set; } = string.Empty;
    public decimal ScoredPoints { get; set; }
    public decimal MaxScore { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
}

public class StudentCourseProgressDto
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int CompletedAssignments { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedModules { get; set; }
    public int TotalModules { get; set; }
    public decimal CompletionPercentage { get; set; }
    public string Status { get; set; } = "Active";
    public decimal? CurrentGrade { get; set; }
}

public class StudentDashboardDto
{
    public decimal CumulativeGpa { get; set; }
    public int EarnedCredits { get; set; }
    public string CurrentSemester { get; set; } = "Summer 2030";
    public int ActiveEnrollmentsCount { get; set; }
    public int CompletedCoursesCount { get; set; }
    public int CertificatesCount { get; set; }
    public int TotalEnrollmentsCount { get; set; }
    public int UnreadNotificationCount { get; set; }

    public IEnumerable<StudentUpcomingClassDto> UpcomingClasses { get; set; } = new List<StudentUpcomingClassDto>();
    public IEnumerable<StudentPendingAssignmentDto> PendingAssignments { get; set; } = new List<StudentPendingAssignmentDto>();
    public IEnumerable<StudentRecentGradeDto> RecentGrades { get; set; } = new List<StudentRecentGradeDto>();
    public IEnumerable<StudentCourseProgressDto> CourseProgress { get; set; } = new List<StudentCourseProgressDto>();
    public IEnumerable<EnrollmentDto> RecentEnrollments { get; set; } = new List<EnrollmentDto>();
    public IEnumerable<ScheduleDto> UpcomingSchedules { get; set; } = new List<ScheduleDto>();
}

public class InstructorTodayClassDto
{
    public int ScheduleId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public string AttendanceStatus { get; set; } = "Pending"; // "Taken", "Pending"
}

public class InstructorPendingGradingDto
{
    public int AssignmentId { get; set; }
    public int CourseId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int SubmissionsCount { get; set; }
    public DateTime DueDate { get; set; }
}

public class InstructorUpcomingSessionDto
{
    public int ScheduleId { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public int EnrolledStudentsCount { get; set; }
}

public class InstructorStudentQuestionDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentAvatar { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public DateTime Date { get; set; }
    public bool IsAnswered { get; set; }
}

public class InstructorDashboardDto
{
    public int AssignedCoursesCount { get; set; }
    public int ActiveStudentsCount { get; set; }
    public int PendingEnrollmentsCount { get; set; }
    public int PendingGradesCount { get; set; }
    public int CertificatesAwardedCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public List<InstructorTodayClassDto> TodaysClasses { get; set; } = new();
    public List<InstructorPendingGradingDto> PendingGrading { get; set; } = new();
    public List<InstructorUpcomingSessionDto> UpcomingSessions { get; set; } = new();
    public List<InstructorStudentQuestionDto> RecentStudentQuestions { get; set; } = new();
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

public class SystemActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "System";
    public string UserAvatar { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
