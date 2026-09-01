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

    public async Task<PaymentStatusBreakdownDto> GetPaymentBreakdownAsync(CancellationToken cancellationToken = default)
    {
        var allEnrollments = await _context.Enrollments
            .AsNoTracking()
            .Select(e => new { e.Status, e.PaymentStatus, e.Fee })
            .ToListAsync(cancellationToken);

        if (!allEnrollments.Any())
            return new PaymentStatusBreakdownDto();

        var totalCount = allEnrollments.Count;

        // Aggregate by payment status
        var paidEnrollments = allEnrollments.Where(e => e.PaymentStatus == "Paid").ToList();
        var pendingEnrollments = allEnrollments.Where(e => e.PaymentStatus == "Pending").ToList();
        var unpaidEnrollments = allEnrollments.Where(e => e.PaymentStatus == "Unpaid").ToList();

        var totalPaidAmount = paidEnrollments.Sum(e => e.Fee);
        var totalPendingAmount = pendingEnrollments.Sum(e => e.Fee);
        var totalUnpaidAmount = unpaidEnrollments.Sum(e => e.Fee);

        // Active/Completed = "Enrolled" category
        var activeStatuses = new[] { EnrollmentStatus.Active, EnrollmentStatus.Completed };
        // Pending = "Applied" category
        // Waitlisted = "Waitlisted" category

        return new PaymentStatusBreakdownDto
        {
            TotalPaid = totalPaidAmount,
            PaidStudentsCount = paidEnrollments.Count,
            PaidPercentage = totalCount > 0 ? Math.Round((paidEnrollments.Count / (decimal)totalCount) * 100, 1) : 0m,

            TotalPending = totalPendingAmount,
            PendingStudentsCount = pendingEnrollments.Count,
            PendingPercentage = totalCount > 0 ? Math.Round((pendingEnrollments.Count / (decimal)totalCount) * 100, 1) : 0m,

            TotalUnpaid = totalUnpaidAmount,
            UnpaidStudentsCount = unpaidEnrollments.Count,
            UnpaidPercentage = totalCount > 0 ? Math.Round((unpaidEnrollments.Count / (decimal)totalCount) * 100, 1) : 0m,

            EnrolledPaid = allEnrollments.Count(e => activeStatuses.Contains(e.Status) && e.PaymentStatus == "Paid"),
            EnrolledPending = allEnrollments.Count(e => activeStatuses.Contains(e.Status) && e.PaymentStatus == "Pending"),
            EnrolledUnpaid = allEnrollments.Count(e => activeStatuses.Contains(e.Status) && e.PaymentStatus == "Unpaid"),

            AppliedPaid = allEnrollments.Count(e => e.Status == EnrollmentStatus.Pending && e.PaymentStatus == "Paid"),
            AppliedPending = allEnrollments.Count(e => e.Status == EnrollmentStatus.Pending && e.PaymentStatus == "Pending"),
            AppliedUnpaid = allEnrollments.Count(e => e.Status == EnrollmentStatus.Pending && e.PaymentStatus == "Unpaid"),

            WaitlistedPaid = allEnrollments.Count(e => e.Status == EnrollmentStatus.Waitlisted && e.PaymentStatus == "Paid"),
            WaitlistedPending = allEnrollments.Count(e => e.Status == EnrollmentStatus.Waitlisted && e.PaymentStatus == "Pending"),
            WaitlistedUnpaid = allEnrollments.Count(e => e.Status == EnrollmentStatus.Waitlisted && e.PaymentStatus == "Unpaid"),
        };
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

        // Check if already enrolled
        var existing = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == dto.StudentId && e.CourseId == dto.CourseId && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Student is already enrolled or has a pending enrollment in this course.");

        var enrollment = new Enrollment
        {
            StudentId = dto.StudentId,
            CourseId = dto.CourseId,
            Batch = string.IsNullOrWhiteSpace(dto.Batch) ? "Batch 1" : dto.Batch,
            Fee = dto.Fee > 0 ? dto.Fee : course.Price,
            PaymentStatus = string.IsNullOrWhiteSpace(dto.PaymentStatus) ? "Pending" : dto.PaymentStatus,
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? "+1 202-555-0142" : dto.PhoneNumber,
            Country = string.IsNullOrWhiteSpace(dto.Country) ? "USA" : dto.Country,
            EnrolledAt = DateTime.UtcNow,
            Status = dto.Status == EnrollmentStatus.Active ? EnrollmentStatus.Active : EnrollmentStatus.Pending
        };

        await _context.Enrollments.AddAsync(enrollment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetEnrollmentByIdAsync(enrollment.Id, cancellationToken)
               ?? _mapper.Map<EnrollmentDto>(enrollment);
    }

    public async Task<bool> UpdateEnrollmentStatusAsync(int id, EnrollmentStatus status, string? paymentStatus = null, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments.FindAsync(new object[] { id }, cancellationToken);
        if (enrollment == null) return false;

        enrollment.Status = status;
        if (!string.IsNullOrWhiteSpace(paymentStatus))
            enrollment.PaymentStatus = paymentStatus;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
