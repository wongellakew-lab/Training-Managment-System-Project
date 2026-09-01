using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.DashboardDtos;
using TmsApi.Application.DTOs.EnrollmentDtos;
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
        var totalCertificates = await _context.Certificates.CountAsync(cancellationToken);

        var pendingList = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => e.Status == EnrollmentStatus.Pending)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentNotifications = await _context.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(6)
            .Select(n => new SystemActivityDto
            {
                Id = n.Id.ToString(),
                Title = n.Title,
                Description = n.Message,
                Type = n.Type,
                Timestamp = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto
        {
            TotalStudents = totalStudents,
            TotalInstructors = totalInstructors,
            TotalCourses = totalCourses,
            ActiveEnrollments = activeEnrollments,
            PendingEnrollments = pendingEnrollments,
            TotalCertificates = totalCertificates,
            PendingApprovalList = _mapper.Map<IEnumerable<EnrollmentDto>>(pendingList),
            RecentActivity = recentNotifications
        };
    }
}
