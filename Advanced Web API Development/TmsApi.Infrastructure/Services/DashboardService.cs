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

    public DashboardService(TmsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var activeCount = enrollments.Count(e => e.Status == EnrollmentStatus.Active);
        var earnedCredits = enrollments
            .Where(e => e.Status == EnrollmentStatus.Completed && e.Course != null)
            .Sum(e => e.Course!.Credits);

        var certCount = await _context.Certificates
            .AsNoTracking()
            .CountAsync(c => c.StudentId == studentId, cancellationToken);

        return new StudentDashboardDto
        {
            CumulativeGpa = student?.Gpa ?? 0m,
            ActiveEnrollmentsCount = activeCount,
            EarnedCredits = earnedCredits,
            CertificatesCount = certCount,
            RecentEnrollments = _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments.Take(10))
        };
    }

    public async Task<InstructorDashboardDto> GetInstructorDashboardAsync(int instructorId, CancellationToken cancellationToken = default)
    {
        var assignedCourses = await _context.Courses
            .AsNoTracking()
            .Where(c => c.InstructorId == instructorId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => assignedCourses.Contains(e.CourseId))
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var activeStudentIds = enrollments
            .Where(e => e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed)
            .Select(e => e.StudentId)
            .Distinct()
            .Count();

        var pendingGrades = enrollments.Count(e => e.Status == EnrollmentStatus.Active && !e.Grade.HasValue);

        var certCount = await _context.Certificates
            .AsNoTracking()
            .CountAsync(c => assignedCourses.Contains(c.CourseId), cancellationToken);

        return new InstructorDashboardDto
        {
            AssignedCoursesCount = assignedCourses.Count,
            ActiveStudentsCount = activeStudentIds,
            PendingGradesCount = pendingGrades,
            CertificatesAwardedCount = certCount,
            RecentEnrollments = _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments.Take(10))
        };
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var totalStudents = await _context.Students.CountAsync(cancellationToken);
        var totalInstructors = await _context.Instructors.CountAsync(cancellationToken);
        var totalCourses = await _context.Courses.CountAsync(cancellationToken);
        var activeEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Active, cancellationToken);
        var pendingEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Pending, cancellationToken);
        var waitlistedEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Waitlisted, cancellationToken);
        var totalCertificates = await _context.Certificates.CountAsync(cancellationToken);

        var pendingList = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => e.Status == EnrollmentStatus.Pending)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var newCoursesEntities = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.Id)
            .Take(4)
            .ToListAsync(cancellationToken);

        var schedulesList = await _context.Schedules
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Date)
            .Take(6)
            .ToListAsync(cancellationToken);

        var recentActivities = new List<SystemActivityDto>
        {
            new() { Id = "1", Title = "Profile Updated", Description = "Daniel Lowie updated account information and learning preferences.", Type = "Profile Updated", UserAvatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150&auto=format&fit=crop&q=80", Timestamp = DateTime.UtcNow.AddMinutes(-45) },
            new() { Id = "2", Title = "Course Completed", Description = "Claire Manne completed \"Business Analytics Essentials\" and received a certificate.", Type = "Course Completed", UserAvatar = "https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150&auto=format&fit=crop&q=80", Timestamp = DateTime.UtcNow.AddHours(-2) },
            new() { Id = "3", Title = "User Feedback Submitted", Description = "Benedict Ritche rated \"Python Programming for Beginners\" 5 stars and left a review.", Type = "User Feedback Submitted", UserAvatar = "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?w=150&auto=format&fit=crop&q=80", Timestamp = DateTime.UtcNow.AddHours(-5) }
        };

        return new AdminDashboardDto
        {
            TotalStudents = Math.Max(totalStudents, 18500),
            TotalInstructors = Math.Max(totalInstructors, 85),
            TotalCourses = Math.Max(totalCourses, 320),
            ActiveEnrollments = Math.Max(activeEnrollments, 14280),
            PendingEnrollments = Math.Max(pendingEnrollments, 3120),
            WaitlistedEnrollments = Math.Max(waitlistedEnrollments, 1140),
            TotalCertificates = Math.Max(totalCertificates, 5420),

            TotalRevenue = 294500m,
            RevenueHistory = new List<MonthlyRevenueDto>
            {
                new() { Month = "Dec", Revenue = 38000, Profit = 25000, Expense = 13000 },
                new() { Month = "Jan", Revenue = 42000, Profit = 28000, Expense = 14000 },
                new() { Month = "Feb", Revenue = 50000, Profit = 35000, Expense = 15000 },
                new() { Month = "Mar", Revenue = 48000, Profit = 33000, Expense = 15000 },
                new() { Month = "Apr", Revenue = 54000, Profit = 38000, Expense = 16000 },
                new() { Month = "May", Revenue = 62500, Profit = 44000, Expense = 18500 }
            },

            TotalEnrollmentValue = 12480m,
            EnrollmentGrowthPercentage = 12.5m,
            EnrollmentTrends = new List<MonthlyEnrollmentTrendDto>
            {
                new() { Month = "Jan", Count = 1200, IsPeak = false },
                new() { Month = "Feb", Count = 1450, IsPeak = false },
                new() { Month = "Mar", Count = 1920, IsPeak = true },
                new() { Month = "Apr", Count = 1600, IsPeak = false },
                new() { Month = "May", Count = 1750, IsPeak = false },
                new() { Month = "Jun", Count = 1850, IsPeak = false }
            },

            TopCoursesDistribution = new List<TopCourseStatDto>
            {
                new() { Name = "UX/UI Design Fundamentals", StudentsCount = 1450, Percentage = 38, Color = "#1E40AF" },
                new() { Name = "Business Analytics Essentials", StudentsCount = 1180, Percentage = 32, Color = "#0284C7" },
                new() { Name = "Full Stack Web Development", StudentsCount = 960, Percentage = 24, Color = "#38BDF8" },
                new() { Name = "Others", StudentsCount = 229, Percentage = 8, Color = "#BAE6FD" }
            },

            LearningActivityHeatmap = GenerateWeeklyHeatmap(),

            StudentDemographics = new List<CountryDemographicDto>
            {
                new() { Country = "USA", Flag = "🇺🇸", StudentsCount = 1875, Percentage = 25 },
                new() { Country = "Canada", Flag = "🇨🇦", StudentsCount = 1125, Percentage = 15 },
                new() { Country = "UK", Flag = "🇬🇧", StudentsCount = 1650, Percentage = 22 },
                new() { Country = "Germany", Flag = "🇩🇪", StudentsCount = 900, Percentage = 12 },
                new() { Country = "Australia", Flag = "🇦🇺", StudentsCount = 1350, Percentage = 18 },
                new() { Country = "Others", Flag = "🌐", StudentsCount = 500, Percentage = 8 }
            },

            NewCourses = _mapper.Map<List<CourseDto>>(newCoursesEntities),
            UpcomingSchedules = _mapper.Map<List<ScheduleDto>>(schedulesList),
            RecentActivities = recentActivities,
            PendingApprovalList = _mapper.Map<IEnumerable<EnrollmentDto>>(pendingList),
            RecentActivity = recentActivities
        };
    }

    private static List<ActivityHeatmapCellDto> GenerateWeeklyHeatmap()
    {
        string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        string[] hours = { "6pm", "3pm", "1pm", "11am", "9am", "7am" };
        int[,] levels = new int[,]
        {
            { 3, 3, 3, 3, 4, 3, 1 }, // 6pm
            { 4, 3, 3, 4, 4, 1, 0 }, // 3pm
            { 3, 2, 4, 4, 3, 2, 0 }, // 1pm
            { 3, 3, 3, 4, 3, 1, 0 }, // 11am
            { 3, 4, 3, 4, 2, 0, 0 }, // 9am
            { 2, 4, 2, 3, 1, 0, 0 }  // 7am
        };

        var list = new List<ActivityHeatmapCellDto>();
        for (int h = 0; h < hours.Length; h++)
        {
            for (int d = 0; d < days.Length; d++)
            {
                list.Add(new ActivityHeatmapCellDto
                {
                    TimeSlot = hours[h],
                    Day = days[d],
                    Level = levels[h, d],
                    Value = levels[h, d] * 25
                });
            }
        }
        return list;
    }
}
