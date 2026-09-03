using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.DashboardDtos;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;

    public EnrollmentService(TmsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResponse<EnrollmentDto>> GetEnrollmentsAsync(
        int page,
        int pageSize,
        int? studentId = null,
        int? courseId = null,
        string? search = null,
        string? status = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .AsNoTracking();

        if (studentId.HasValue)
            query = query.Where(e => e.StudentId == studentId.Value);

        if (courseId.HasValue)
            query = query.Where(e => e.CourseId == courseId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e =>
                (e.Student != null && (e.Student.FirstName.ToLower().Contains(s) ||
                                       e.Student.LastName.ToLower().Contains(s) ||
                                       e.Student.Email.ToLower().Contains(s))) ||
                (e.Course != null && (e.Course.Title.ToLower().Contains(s) ||
                                      e.Course.Code.ToLower().Contains(s))) ||
                e.Batch.ToLower().Contains(s) ||
                e.PhoneNumber.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All" && status != "Any Status")
        {
            var s = status.Trim().ToLower();
            if (s == "enrolled" || s == "active" || s == "approved")
                query = query.Where(e => e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed);
            else if (s == "applied" || s == "pending")
                query = query.Where(e => e.Status == EnrollmentStatus.Pending);
            else if (s == "waitlisted")
                query = query.Where(e => e.Status == EnrollmentStatus.Waitlisted);
            else if (s == "completed")
                query = query.Where(e => e.Status == EnrollmentStatus.Completed);
            else if (s == "dropped")
                query = query.Where(e => e.Status == EnrollmentStatus.Dropped);
            else if (s == "rejected")
                query = query.Where(e => e.Status == EnrollmentStatus.Rejected);
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All" && category != "All Category")
        {
            var cat = category.Trim().ToLower();
            query = query.Where(e => e.Course != null && e.Course.Category.ToLower() == cat);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.EnrolledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<EnrollmentDto>(_mapper.Map<IEnumerable<EnrollmentDto>>(items), page, pageSize, totalCount);
    }



    public async Task<EnrollmentDto?> GetEnrollmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

        return enrollment == null ? null : _mapper.Map<EnrollmentDto>(enrollment);
    }

    public async Task<EnrollmentDto> CreateEnrollmentAsync(CreateEnrollmentDto dto, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.FindAsync(new object[] { dto.StudentId }, cancellationToken);
        if (student == null)
            throw new InvalidOperationException($"Student with ID {dto.StudentId} not found.");

        var course = await _context.Courses.FindAsync(new object[] { dto.CourseId }, cancellationToken);
        if (course == null)
            throw new InvalidOperationException($"Course with ID {dto.CourseId} not found.");

        // Check course status
        if (course.Status != "Active")
            throw new InvalidOperationException($"Cannot enroll in course '{course.Title}' because it is currently {course.Status}.");

        // Check enrollment deadline if set
        if (course.EnrollmentDeadline.HasValue && DateTime.UtcNow > course.EnrollmentDeadline.Value)
            throw new InvalidOperationException($"The enrollment period for '{course.Title}' closed on {course.EnrollmentDeadline.Value:MMM dd, yyyy}.");

        // Check if already enrolled
        var existing = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId && e.CourseId == dto.CourseId && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Student is already enrolled or has a pending enrollment in this course.");

        // Check prerequisites
        if (!string.IsNullOrWhiteSpace(course.Prerequisites) && !course.Prerequisites.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            var prereqSearch = course.Prerequisites.Trim().ToLower();
            var hasCompletedPrereq = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                .AnyAsync(e => e.StudentId == dto.StudentId
                               && (e.Status == EnrollmentStatus.Completed || (e.Grade.HasValue && e.Grade.Value >= 2.0m))
                               && e.Course != null
                               && (e.Course.Code.ToLower().Contains(prereqSearch) || e.Course.Title.ToLower().Contains(prereqSearch)),
                    cancellationToken);

            if (!hasCompletedPrereq)
            {
                throw new InvalidOperationException($"Prerequisite '{course.Prerequisites}' has not been completed yet.");
            }
        }

        // Check course capacity and prevent over-enrollment
        var activeOrPendingCount = await _context.Enrollments
            .CountAsync(e => e.CourseId == dto.CourseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Pending) && !e.IsDeleted, cancellationToken);

        if (activeOrPendingCount >= course.MaxCapacity)
            throw new InvalidOperationException("Course is full. No additional students can enroll.");

        // Keep course.EnrolledStudentsCount in sync; updating Course ensures row-version optimistic concurrency check executes
        course.EnrolledStudentsCount = activeOrPendingCount + 1;

        var enrollment = new Enrollment
        {
            StudentId = dto.StudentId,
            CourseId = dto.CourseId,
            Batch = string.IsNullOrWhiteSpace(dto.Batch) ? "Batch 1" : dto.Batch,
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? "+1 202-555-0142" : dto.PhoneNumber,
            Country = string.IsNullOrWhiteSpace(dto.Country) ? "USA" : dto.Country,
            EnrolledAt = DateTime.UtcNow,
            Status = dto.Status != 0 ? dto.Status : EnrollmentStatus.Active
        };

        await _context.Enrollments.AddAsync(enrollment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetEnrollmentByIdAsync(enrollment.Id, cancellationToken)
               ?? _mapper.Map<EnrollmentDto>(enrollment);
    }

    public async Task<bool> UpdateEnrollmentStatusAsync(int id, EnrollmentStatus status, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments.FindAsync(new object[] { id }, cancellationToken);
        if (enrollment == null) return false;

        enrollment.Status = status;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
