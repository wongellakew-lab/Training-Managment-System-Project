using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
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

    public async Task<PagedResponse<EnrollmentDto>> GetEnrollmentsAsync(int page, int pageSize, int? studentId = null, int? courseId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .AsNoTracking();

        if (studentId.HasValue)
        {
            query = query.Where(e => e.StudentId == studentId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(e => e.CourseId == courseId.Value);
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
        var studentExists = await _context.Students.AnyAsync(s => s.Id == dto.StudentId, cancellationToken);
        if (!studentExists)
        {
            throw new InvalidOperationException($"Student with ID {dto.StudentId} not found.");
        }

        var course = await _context.Courses.FindAsync(new object[] { dto.CourseId }, cancellationToken);
        if (course == null)
        {
            throw new InvalidOperationException($"Course with ID {dto.CourseId} not found.");
        }

        var alreadyEnrolled = await _context.Enrollments.AnyAsync(
            e => e.StudentId == dto.StudentId && e.CourseId == dto.CourseId, cancellationToken);

        if (alreadyEnrolled)
        {
            throw new InvalidOperationException("Student is already enrolled in this course.");
        }

        var activeEnrollmentCount = await _context.Enrollments.CountAsync(
            e => e.CourseId == dto.CourseId && e.Status != EnrollmentStatus.Rejected, cancellationToken);

        if (activeEnrollmentCount >= course.MaxCapacity)
        {
            throw new InvalidOperationException($"Course capacity limit of {course.MaxCapacity} reached.");
        }

        var enrollment = new Enrollment
        {
            StudentId = dto.StudentId,
            CourseId = dto.CourseId,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Approved
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
