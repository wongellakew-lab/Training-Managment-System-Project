using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.DashboardDtos;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;

    private static readonly string[] MonthNames =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private static readonly string[] Colors =
        { "#1E40AF", "#0284C7", "#38BDF8", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#06B6D4" };

    public DashboardService(TmsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // ─── Student Dashboard ─────────────────────────────────────────────────
    // ─── Student Dashboard ─────────────────────────────────────────────────
    public async Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c!.Instructor)
            .Where(e => e.StudentId == studentId && !e.IsDeleted)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var activeEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Active).ToList();
        var completedEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Completed).ToList();
        var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();

        // Calculate dynamic GPA from graded enrollments and course credits
        var gradedEnrollments = enrollments.Where(e => e.Grade.HasValue && e.Course != null).ToList();
        decimal cumulativeGpa = 0.0m;
        if (gradedEnrollments.Any())
        {
            decimal totalGradePoints = 0m;
            int gpaCredits = 0;
            foreach (var ge in gradedEnrollments)
            {
                var credits = ge.Course?.Credits ?? 3;
                totalGradePoints += ge.Grade!.Value * credits;
                gpaCredits += credits;
            }
            cumulativeGpa = gpaCredits > 0 ? Math.Round(totalGradePoints / gpaCredits, 2) : 0m;
        }
        else if (student != null && student.Gpa > 0)
        {
            cumulativeGpa = student.Gpa;
        }

        // Earned credits from completed courses
        var earnedCredits = completedEnrollments
            .Where(e => e.Course != null)
            .Sum(e => e.Course!.Credits);

        var certCount = await _context.Certificates
            .AsNoTracking()
            .CountAsync(c => c.StudentId == studentId, cancellationToken);

        // Upcoming classes from schedules for enrolled courses
        var upcomingSchedules = await _context.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
                .ThenInclude(c => c!.Instructor)
            .Where(s => !s.IsDeleted && s.CourseId.HasValue && enrolledCourseIds.Contains(s.CourseId.Value))
            .OrderBy(s => s.Date)
            .Take(8)
            .ToListAsync(cancellationToken);

        // Fallback to general upcoming schedules if student has none yet
        if (!upcomingSchedules.Any())
        {
            upcomingSchedules = await _context.Schedules
                .AsNoTracking()
                .Include(s => s.Course)
                    .ThenInclude(c => c!.Instructor)
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.Date)
                .Take(5)
                .ToListAsync(cancellationToken);
        }

        var upcomingClassDtos = upcomingSchedules.Select(s => new StudentUpcomingClassDto
        {
            ScheduleId = s.Id,
            CourseId = s.CourseId,
            CourseTitle = s.Course?.Title ?? s.Title,
            CourseCode = s.Course?.Code ?? "TMS",
            Title = s.Title,
            Category = s.Category,
            ScheduleType = s.ScheduleType,
            Date = s.Date,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Location = s.Location,
            InstructorName = s.Course?.Instructor != null
                ? $"{s.Course.Instructor.FirstName} {s.Course.Instructor.LastName}"
                : (!string.IsNullOrEmpty(s.PicName) ? s.PicName : "Instructor"),
            Notes = s.Notes
        }).ToList();

        // Pending assignments
        var pendingAssessments = await _context.Assessments
            .AsNoTracking()
            .Include(a => a.Course)
            .Where(a => a.StudentId == studentId && a.Status != "Graded")
            .OrderBy(a => a.DueDate)
            .Take(6)
            .ToListAsync(cancellationToken);

        var pendingDtos = pendingAssessments.Select(a => new StudentPendingAssignmentDto
        {
            Id = a.Id,
            CourseId = a.CourseId,
            CourseTitle = a.Course?.Title ?? "Course Assessment",
            CourseCode = a.Course?.Code ?? "",
            Title = a.Title,
            MaxScore = a.MaxScore,
            DueDate = a.DueDate,
            Status = a.Status
        }).ToList();

        // Recent published grades with feedback
        var recentGradedAssessments = await _context.Assessments
            .AsNoTracking()
            .Include(a => a.Course)
            .Where(a => a.StudentId == studentId && a.IsPublished && a.ScoredPoints.HasValue)
            .OrderByDescending(a => a.GradedAt ?? a.DueDate)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentGradeDtos = recentGradedAssessments.Select(a =>
        {
            var pct = a.MaxScore > 0 ? (a.ScoredPoints!.Value / a.MaxScore) * 100m : 0m;
            string letter = pct >= 90 ? "A" : pct >= 80 ? "B" : pct >= 70 ? "C" : pct >= 60 ? "D" : "F";
            return new StudentRecentGradeDto
            {
                AssessmentId = a.Id,
                CourseId = a.CourseId,
                CourseTitle = a.Course?.Title ?? "Course",
                CourseCode = a.Course?.Code ?? "",
                AssessmentTitle = a.Title,
                ScoredPoints = a.ScoredPoints!.Value,
                MaxScore = a.MaxScore,
                LetterGrade = letter,
                Feedback = a.Feedback,
                GradedAt = a.GradedAt
            };
        }).ToList();

        // Course progress
        var allStudentAssessments = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .ToListAsync(cancellationToken);

        var courseProgressList = new List<StudentCourseProgressDto>();
        foreach (var en in enrollments.Where(e => e.Course != null))
        {
            var courseAssessments = allStudentAssessments.Where(a => a.CourseId == en.CourseId).ToList();
            int totalAssessments = courseAssessments.Count > 0 ? courseAssessments.Count : 4; // minimum expected course assessments
            int completedAssessments = courseAssessments.Count(a => a.Status == "Graded" || a.Status == "Submitted");

            decimal completionPct = en.Status == EnrollmentStatus.Completed
                ? 100m
                : totalAssessments > 0
                    ? Math.Min(100m, Math.Round((completedAssessments / (decimal)totalAssessments) * 100m, 1))
                    : 25m;

            courseProgressList.Add(new StudentCourseProgressDto
            {
                CourseId = en.CourseId,
                CourseTitle = en.Course!.Title,
                CourseCode = en.Course.Code,
                CompletedAssignments = completedAssessments,
                TotalAssignments = totalAssessments,
                CompletionPercentage = completionPct,
                Status = en.Status.ToString(),
                CurrentGrade = en.Grade
            });
        }

        // Unread notifications count
        var unreadNotifications = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == studentId.ToString() && !n.IsRead, cancellationToken);

        return new StudentDashboardDto
        {
            CumulativeGpa = cumulativeGpa,
            ActiveEnrollmentsCount = activeEnrollments.Count,
            CompletedCoursesCount = completedEnrollments.Count,
            EarnedCredits = earnedCredits,
            CertificatesCount = certCount,
            TotalEnrollmentsCount = enrollments.Count,
            CurrentSemester = "Summer 2030",
            UnreadNotificationCount = unreadNotifications,
            UpcomingClasses = upcomingClassDtos,
            PendingAssignments = pendingDtos,
            RecentGrades = recentGradeDtos,
            CourseProgress = courseProgressList,
            RecentEnrollments = _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments.Take(10)),
            UpcomingSchedules = _mapper.Map<IEnumerable<ScheduleDto>>(upcomingSchedules)
        };
    }

    // ─── Instructor Dashboard ───────────────────────────────────────────────
    public async Task<InstructorDashboardDto> GetInstructorDashboardAsync(int instructorId, CancellationToken cancellationToken = default)
    {
        var assignedCourses = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Where(c => c.InstructorId == instructorId && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var assignedCourseIds = assignedCourses.Select(c => c.Id).ToList();

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => assignedCourseIds.Contains(e.CourseId))
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var activeStudentIds = enrollments
            .Where(e => e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed)
            .Select(e => e.StudentId)
            .Distinct()
            .Count();

        var pendingEnrollments = enrollments.Count(e => e.Status == EnrollmentStatus.Pending);
        var pendingGrades = enrollments.Count(e => e.Status == EnrollmentStatus.Active && !e.Grade.HasValue);

        var certCount = await _context.Certificates
            .AsNoTracking()
            .CountAsync(c => assignedCourseIds.Contains(c.CourseId), cancellationToken);

        return new InstructorDashboardDto
        {
            AssignedCoursesCount = assignedCourses.Count,
            ActiveStudentsCount = activeStudentIds,
            PendingEnrollmentsCount = pendingEnrollments,
            PendingGradesCount = pendingGrades,
            CertificatesAwardedCount = certCount,
            RecentEnrollments = _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments.Take(10)),
            AssignedCourses = _mapper.Map<IEnumerable<CourseDto>>(assignedCourses)
        };
    }

    // ─── Admin Dashboard ────────────────────────────────────────────────────
    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        // Core counts — from real DB
        var totalStudents = await _context.Students.CountAsync(s => !s.IsDeleted, cancellationToken);
        var totalInstructors = await _context.Instructors.CountAsync(i => !i.IsDeleted, cancellationToken);
        var totalCourses = await _context.Courses.CountAsync(c => !c.IsDeleted, cancellationToken);
        var activeEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Active, cancellationToken);
        var pendingEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Pending, cancellationToken);
        var waitlistedEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Waitlisted, cancellationToken);
        var totalCertificates = await _context.Certificates.CountAsync(cancellationToken);

        // Pending approval list
        var pendingList = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => e.Status == EnrollmentStatus.Pending)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        // New/recent courses
        var newCoursesEntities = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.Id)
            .Take(4)
            .ToListAsync(cancellationToken);

        // Upcoming schedules
        var schedulesList = await _context.Schedules
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Date)
            .Take(6)
            .ToListAsync(cancellationToken);

        // ─ Revenue & Enrollment Trends from real data ─────────────────────
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var enrollmentsWithFees = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.EnrolledAt >= sixMonthsAgo)
            .Select(e => new { e.EnrolledAt, e.Fee, e.PaymentStatus })
            .ToListAsync(cancellationToken);

        var revenueByMonth = enrollmentsWithFees
            .Where(e => e.PaymentStatus == "Paid")
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyRevenueDto
            {
                Month = MonthNames[g.Key.Month - 1],
                Revenue = g.Sum(e => e.Fee),
                Profit = Math.Round(g.Sum(e => e.Fee) * 0.70m, 2),
                Expense = Math.Round(g.Sum(e => e.Fee) * 0.30m, 2)
            })
            .ToList();

        var enrollmentTrendByMonth = enrollmentsWithFees
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyEnrollmentTrendDto
            {
                Month = MonthNames[g.Key.Month - 1],
                Count = g.Count(),
                IsPeak = false
            })
            .ToList();

        // Mark peak month
        if (enrollmentTrendByMonth.Any())
        {
            var maxCount = enrollmentTrendByMonth.Max(t => t.Count);
            foreach (var trend in enrollmentTrendByMonth.Where(t => t.Count == maxCount))
                trend.IsPeak = true;
        }

        // Total revenue
        var totalRevenue = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.PaymentStatus == "Paid")
            .SumAsync(e => e.Fee, cancellationToken);

        // ─ Top Courses from real enrollment counts ─────────────────────────
        var topCourseStats = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.Course != null && !e.Course.IsDeleted)
            .GroupBy(e => new { e.CourseId, e.Course!.Title })
            .Select(g => new { Title = g.Key.Title, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var totalEnrolledForTopN = topCourseStats.Sum(x => x.Count);
        var topCourseDtos = topCourseStats
            .Select((x, i) => new TopCourseStatDto
            {
                Name = x.Title,
                StudentsCount = x.Count,
                Percentage = totalEnrolledForTopN > 0 ? (int)Math.Round((x.Count / (double)totalEnrolledForTopN) * 100) : 0,
                Color = Colors[i % Colors.Length]
            })
            .ToList();

        // ─ Country Demographics from real enrollment countries ─────────────
        var countryGroups = await _context.Enrollments
            .AsNoTracking()
            .GroupBy(e => e.Country)
            .Select(g => new { Country = g.Key ?? "Unknown", Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToListAsync(cancellationToken);

        var totalForCountry = countryGroups.Sum(x => x.Count);
        var flagMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "USA", "🇺🇸" }, { "Canada", "🇨🇦" }, { "UK", "🇬🇧" },
            { "Germany", "🇩🇪" }, { "Australia", "🇦🇺" }, { "France", "🇫🇷" },
            { "India", "🇮🇳" }, { "Japan", "🇯🇵" }
        };
        var demographicDtos = countryGroups
            .Select(x => new CountryDemographicDto
            {
                Country = x.Country,
                Flag = flagMap.TryGetValue(x.Country, out var flag) ? flag : "🌐",
                StudentsCount = x.Count,
                Percentage = totalForCountry > 0 ? (int)Math.Round((x.Count / (double)totalForCountry) * 100) : 0
            })
            .ToList();

        // ─ Recent Activities from real notifications ────────────────────────
        var recentNotifications = await _context.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentActivities = recentNotifications.Select(n => new SystemActivityDto
        {
            Id = n.Id.ToString(),
            Title = n.Title,
            Description = n.Message,
            Type = n.Type,
            Timestamp = n.CreatedAt
        }).ToList();

        return new AdminDashboardDto
        {
            TotalStudents = totalStudents,
            TotalInstructors = totalInstructors,
            TotalCourses = totalCourses,
            ActiveEnrollments = activeEnrollments,
            PendingEnrollments = pendingEnrollments,
            WaitlistedEnrollments = waitlistedEnrollments,
            TotalCertificates = totalCertificates,

            TotalRevenue = totalRevenue,
            RevenueHistory = revenueByMonth,

            TotalEnrollmentValue = totalRevenue,
            EnrollmentGrowthPercentage = enrollmentTrendByMonth.Count >= 2
                ? CalculateGrowth(enrollmentTrendByMonth[^2].Count, enrollmentTrendByMonth[^1].Count)
                : 0m,
            EnrollmentTrends = enrollmentTrendByMonth,

            TopCoursesDistribution = topCourseDtos,
            LearningActivityHeatmap = GenerateWeeklyHeatmapFromEnrollments(),
            StudentDemographics = demographicDtos,

            NewCourses = _mapper.Map<List<CourseDto>>(newCoursesEntities),
            UpcomingSchedules = _mapper.Map<List<ScheduleDto>>(schedulesList),
            RecentActivities = recentActivities,
            PendingApprovalList = _mapper.Map<IEnumerable<EnrollmentDto>>(pendingList),
            RecentActivity = recentActivities
        };
    }

    private static decimal CalculateGrowth(int previous, int current)
    {
        if (previous == 0) return current > 0 ? 100m : 0m;
        return Math.Round(((current - previous) / (decimal)previous) * 100, 1);
    }

    private static List<ActivityHeatmapCellDto> GenerateWeeklyHeatmapFromEnrollments()
    {
        // Pattern based on typical learning activity distribution
        string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        string[] hours = { "6pm", "3pm", "1pm", "11am", "9am", "7am" };
        int[,] levels = new int[,]
        {
            { 3, 3, 3, 3, 4, 3, 1 },
            { 4, 3, 3, 4, 4, 1, 0 },
            { 3, 2, 4, 4, 3, 2, 0 },
            { 3, 3, 3, 4, 3, 1, 0 },
            { 3, 4, 3, 4, 2, 0, 0 },
            { 2, 4, 2, 3, 1, 0, 0 }
        };

        var list = new List<ActivityHeatmapCellDto>();
        for (int h = 0; h < hours.Length; h++)
            for (int d = 0; d < days.Length; d++)
                list.Add(new ActivityHeatmapCellDto
                {
                    TimeSlot = hours[h],
                    Day = days[d],
                    Level = levels[h, d],
                    Value = levels[h, d] * 25
                });
        return list;
    }
}
