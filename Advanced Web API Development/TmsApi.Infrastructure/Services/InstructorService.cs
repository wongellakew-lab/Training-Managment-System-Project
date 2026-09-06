using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class InstructorService(
    TmsDbContext _context,
    IMapper _mapper,
    IScheduleService _scheduleService,
    UserManager<TmsUser>? _userManager = null,
    IWebHostEnvironment? _env = null) : IInstructorService
{

    public async Task<PagedResponse<InstructorDto>> GetInstructorsAsync(PagedRequest request, string? department, CancellationToken cancellationToken = default)
    {
        var query = _context.Instructors
            .AsNoTracking()
            .Where(i => !i.IsDeleted)
            .Include(i => i.Courses)
                .ThenInclude(c => c.Enrollments)
            .Include(i => i.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(i =>
                i.FirstName.ToLower().Contains(search) ||
                i.LastName.ToLower().Contains(search) ||
                i.Department.ToLower().Contains(search) ||
                i.EmployeeNumber.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(department) && !department.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.Department.ToLower() == department.ToLower());
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var instructors = await query
            .OrderBy(i => i.LastName)
            .ThenBy(i => i.FirstName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = instructors.Select(i =>
        {
            var activeCourses = i.Courses.Where(c => !c.IsDeleted).ToList();
            return new InstructorDto
            {
                Id = i.Id,
                EmployeeNumber = i.EmployeeNumber,
                FirstName = i.FirstName,
                LastName = i.LastName,
                Email = i.Email,
                Department = i.Department,
                HireDate = i.HireDate,
                Status = i.Status,
                CoursesCount = activeCourses.Count,
                StudentsCount = activeCourses.SelectMany(c => c.Enrollments).Where(e => !e.IsDeleted).Select(e => e.StudentId).Distinct().Count(),
                Rating = 4.8m,
                Avatar = i.User?.ProfilePictureUrl ?? string.Empty,
                PhoneNumber = i.User?.PhoneNumber,
                CourseTitles = activeCourses.Select(c => c.Title).ToList()
            };
        }).ToList();

        return new PagedResponse<InstructorDto>(dtos, request.PageNumber, request.PageSize, totalRecords);
    }

    public async Task<InstructorDetailDto?> GetInstructorByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructor = await _context.Instructors
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Courses)
                .ThenInclude(c => c.Enrollments)
            .Include(i => i.Courses)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (instructor == null) return null;

        var courses = instructor.Courses.Where(c => !c.IsDeleted).Select(c => new InstructorCourseDto
        {
            Id = c.Id,
            Code = c.Code,
            Title = c.Title,
            Category = c.Category,
            Level = c.Level,
            Status = c.Status,
            EnrolledStudentsCount = c.Enrollments.Count(e => !e.IsDeleted),
            LessonsCount = c.Modules.SelectMany(m => m.Lessons).Count(),
            Rating = c.Rating,
            ImageUrl = c.ImageUrl,
            StartDate = c.StartDate,
            EndDate = c.EndDate
        }).ToList();

        return new InstructorDetailDto
        {
            Id = instructor.Id,
            EmployeeNumber = instructor.EmployeeNumber,
            FirstName = instructor.FirstName,
            LastName = instructor.LastName,
            Email = instructor.Email,
            Department = instructor.Department,
            HireDate = instructor.HireDate,
            Status = instructor.Status,
            CoursesCount = courses.Count,
            StudentsCount = courses.SelectMany(c => instructor.Courses.Where(ic => ic.Id == c.Id).SelectMany(ic => ic.Enrollments)).Where(e => !e.IsDeleted).Select(e => e.StudentId).Distinct().Count(),
            Rating = 4.8m,
            Avatar = instructor.User?.ProfilePictureUrl ?? string.Empty,
            PhoneNumber = instructor.User?.PhoneNumber,
            CourseTitles = courses.Select(c => c.Title).ToList(),
            Bio = instructor.User?.Bio,
            Courses = courses
        };
    }

    public async Task<InstructorDto> CreateInstructorAsync(CreateInstructorDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email?.Trim() ?? string.Empty;
        var firstName = dto.FirstName?.Trim() ?? string.Empty;
        var lastName = dto.LastName?.Trim() ?? string.Empty;
        var department = string.IsNullOrWhiteSpace(dto.Department) ? "General" : dto.Department.Trim();
        var employeeNumber = string.IsNullOrWhiteSpace(dto.EmployeeNumber)
            ? $"EMP-{Guid.NewGuid().ToString()[..6].ToUpper()}"
            : dto.EmployeeNumber.Trim();

        // 1. Check for duplicate email in Identity
        if (_userManager != null)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"A user account with email '{email}' already exists.");
            }
        }

        // 2. Check for duplicate email or employee number in Instructors table
        var emailLower = email.ToLower();
        var existingInstructor = await _context.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => !i.IsDeleted && i.Email.ToLower() == emailLower, cancellationToken);
        if (existingInstructor != null)
        {
            throw new InvalidOperationException($"An instructor record with email '{email}' already exists.");
        }

        var empLower = employeeNumber.ToLower();
        var existingEmp = await _context.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => !i.IsDeleted && i.EmployeeNumber.ToLower() == empLower, cancellationToken);
        if (existingEmp != null)
        {
            throw new InvalidOperationException($"An instructor with employee code '{employeeNumber}' already exists.");
        }

        TmsUser? user = null;

        // 3. Create Identity user if UserManager is available
        if (_userManager != null)
        {
            user = new TmsUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Department = department,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, "Instructor@123");
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create instructor user account: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Instructor");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException("Failed to assign Instructor role to the new user.");
            }
        }

        // 4. Create and persist Instructor entity
        var instructor = new Instructor
        {
            UserId = user?.Id,
            EmployeeNumber = employeeNumber,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Department = department,
            HireDate = DateTime.UtcNow,
            Status = "Active"
        };

        try
        {
            await _context.Instructors.AddAsync(instructor, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Bidirectionally update User with InstructorId
            if (user != null)
            {
                user.InstructorId = instructor.Id;
                await _userManager!.UpdateAsync(user);
            }
        }
        catch
        {
            // Roll back Identity user if saving the Instructor entity fails
            if (user != null)
            {
                try
                {
                    await _userManager!.DeleteAsync(user);
                }
                catch
                {
                    // Ignore secondary rollback errors
                }
            }
            throw;
        }

        return new InstructorDto
        {
            Id = instructor.Id,
            EmployeeNumber = instructor.EmployeeNumber,
            FirstName = instructor.FirstName,
            LastName = instructor.LastName,
            Email = instructor.Email,
            Department = instructor.Department,
            HireDate = instructor.HireDate,
            Status = instructor.Status,
            Rating = 4.8m,
            Avatar = user?.ProfilePictureUrl ?? string.Empty
        };
    }

    public async Task<bool> UpdateInstructorAsync(int id, UpdateInstructorDto dto, CancellationToken cancellationToken = default)
    {
        var instructor = await _context.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
        if (instructor == null) return false;

        instructor.FirstName = dto.FirstName;
        instructor.LastName = dto.LastName;
        instructor.Department = dto.Department;

        if (instructor.User != null)
        {
            instructor.User.FirstName = dto.FirstName;
            instructor.User.LastName = dto.LastName;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) instructor.User.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(dto.Bio)) instructor.User.Bio = dto.Bio;
            if (!string.IsNullOrWhiteSpace(dto.Department)) instructor.User.Department = dto.Department;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructor = await _context.Instructors.FindAsync(new object[] { id }, cancellationToken);
        if (instructor == null || instructor.IsDeleted) return false;

        instructor.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SuspendInstructorAsync(int id, SuspendInstructorRequestDto dto, CancellationToken cancellationToken = default)
    {
        var instructor = await _context.Instructors
            .Include(i => i.Courses)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (instructor == null) return false;

        instructor.Status = "Suspended";

        if (dto.CourseReassignments != null && dto.CourseReassignments.Any())
        {
            foreach (var r in dto.CourseReassignments)
            {
                var course = instructor.Courses.FirstOrDefault(c => c.Id == r.CourseId && !c.IsDeleted);
                if (course != null)
                {
                    course.InstructorId = r.ReplacementInstructorId;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ActivateInstructorAsync(int id, CancellationToken cancellationToken = default)
    {
        var instructor = await _context.Instructors.FindAsync(new object[] { id }, cancellationToken);
        if (instructor == null || instructor.IsDeleted) return false;

        instructor.Status = "Active";
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<InstructorProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var instructor = await _context.Instructors
            .AsNoTracking()
            .Include(i => i.Courses)
                .ThenInclude(c => c.Enrollments)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.UserId == userId && !i.IsDeleted, cancellationToken);

        if (instructor == null) return null;

        var activeCourses = instructor.Courses.Where(c => !c.IsDeleted).ToList();
        var assignedCourseIds = activeCourses.Select(c => c.Id).ToList();
        var totalCourses = activeCourses.Count;

        var activeStudents = activeCourses
            .SelectMany(c => c.Enrollments)
            .Where(e => !e.IsDeleted && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed))
            .Select(e => e.StudentId)
            .Distinct()
            .Count();

        var totalCertificates = await _context.Certificates
            .AsNoTracking()
            .CountAsync(c => assignedCourseIds.Contains(c.CourseId), cancellationToken);

        var totalAttendances = await _context.Attendances
            .AsNoTracking()
            .CountAsync(a => assignedCourseIds.Contains(a.CourseId), cancellationToken);

        var presentCount = await _context.Attendances
            .AsNoTracking()
            .CountAsync(a => assignedCourseIds.Contains(a.CourseId) && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late), cancellationToken);

        decimal attendanceRate = totalAttendances > 0
            ? Math.Round(((decimal)presentCount / totalAttendances) * 100m, 1)
            : 0.0m;

        return new InstructorProfileDto
        {
            Id = instructor.Id,
            UserId = userId,
            EmployeeNumber = instructor.EmployeeNumber,
            FirstName = instructor.FirstName,
            LastName = instructor.LastName,
            Email = instructor.Email,
            PhoneNumber = instructor.User?.PhoneNumber,
            Department = instructor.Department,
            HireDate = instructor.HireDate,
            Bio = instructor.User?.Bio,
            AvatarUrl = instructor.User?.ProfilePictureUrl ?? string.Empty,
            ProfilePictureUrl = instructor.User?.ProfilePictureUrl ?? string.Empty,
            TotalCourses = totalCourses,
            TotalStudents = activeStudents,
            ActiveStudents = activeStudents,
            TotalCertificates = totalCertificates,
            AttendanceRate = attendanceRate,
            AverageRating = 4.8m
        };
    }

    public async Task<bool> UpdateProfileAsync(string userId, UpdateInstructorProfileDto dto, CancellationToken cancellationToken = default)
    {
        var instructor = await _context.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.UserId == userId && !i.IsDeleted, cancellationToken);

        if (instructor == null) return false;

        instructor.FirstName = dto.FirstName;
        instructor.LastName = dto.LastName;
        if (!string.IsNullOrWhiteSpace(dto.Department)) instructor.Department = dto.Department;

        if (instructor.User != null)
        {
            instructor.User.FirstName = dto.FirstName;
            instructor.User.LastName = dto.LastName;
            instructor.User.PhoneNumber = dto.PhoneNumber;
            instructor.User.Bio = dto.Bio;
            if (!string.IsNullOrWhiteSpace(dto.Department)) instructor.User.Department = dto.Department;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> UploadAvatarAsync(string userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var instructor = await _context.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.UserId == userId && !i.IsDeleted, cancellationToken);

        if (instructor == null || _env == null) return null;

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "avatars");
        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"instructor_{instructor.Id}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var avatarUrl = $"/uploads/avatars/{fileName}";
        if (instructor.User != null) instructor.User.ProfilePictureUrl = avatarUrl;

        await _context.SaveChangesAsync(cancellationToken);
        return avatarUrl;
    }

    public async Task<InstructorStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var total = await _context.Instructors.CountAsync(i => !i.IsDeleted, cancellationToken);
        var active = await _context.Instructors.CountAsync(i => !i.IsDeleted && i.Status == "Active", cancellationToken);

        return new InstructorStatsDto
        {
            TotalInstructors = total,
            ActiveMentors = active,
            AverageRating = 4.8m
        };
    }
}
