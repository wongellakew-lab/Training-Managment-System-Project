using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class InstructorsController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;
    private readonly IScheduleService _scheduleService;
    private readonly UserManager<TmsUser> _userManager;

    public InstructorsController(
        TmsDbContext context,
        IMapper mapper,
        IScheduleService scheduleService,
        UserManager<TmsUser> userManager)
    {
        _context = context;
        _mapper = mapper;
        _scheduleService = scheduleService;
        _userManager = userManager;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetInstructors(
        [FromQuery] PagedRequest request,
        [FromQuery] string? department,
        CancellationToken cancellationToken)
    {
        var query = _context.Instructors
            .AsNoTracking()
            .Where(i => !i.IsDeleted)
            .Include(i => i.Courses.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Enrollments)
            .Include(i => i.User);

        IQueryable<Instructor> filteredQuery = query;

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            filteredQuery = filteredQuery.Where(i =>
                i.FirstName.ToLower().Contains(search) ||
                i.LastName.ToLower().Contains(search) ||
                i.Department.ToLower().Contains(search) ||
                i.EmployeeNumber.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(department) && !department.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var deptLower = department.ToLower();
            filteredQuery = filteredQuery.Where(i => i.Department.ToLower() == deptLower);
        }

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var items = await filteredQuery
            .OrderBy(i => i.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(i =>
        {
            var dto = _mapper.Map<InstructorDto>(i);
            dto.Status = string.IsNullOrEmpty(i.Status) ? "Active" : i.Status;
            dto.CoursesCount = i.Courses.Count(c => !c.IsDeleted);
            dto.StudentsCount = i.Courses
                .Where(c => !c.IsDeleted)
                .SelectMany(c => c.Enrollments)
                .Select(e => e.StudentId)
                .Distinct()
                .Count();
            dto.Avatar = !string.IsNullOrEmpty(i.User?.ProfilePictureUrl)
                ? i.User.ProfilePictureUrl
                : "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80";
            dto.PhoneNumber = i.User?.PhoneNumber;
            dto.CourseTitles = i.Courses
                .Where(c => !c.IsDeleted)
                .Select(c => c.Title)
                .ToList();
            return dto;
        }).ToList();

        return Ok(new PagedResponse<InstructorDto>(dtos, request.Page, request.PageSize, totalCount));
    }

    [Authorize]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var totalInstructors = await _context.Instructors.CountAsync(i => !i.IsDeleted, cancellationToken);
        var activeMentors = await _context.Courses
            .Where(c => !c.IsDeleted && c.InstructorId.HasValue)
            .Select(c => c.InstructorId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

        return Ok(new InstructorStatsDto
        {
            TotalInstructors = totalInstructors,
            ActiveMentors = activeMentors,
            AverageRating = 4.85m
        });
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetInstructorById(int id, CancellationToken cancellationToken)
    {
        var instructor = await _context.Instructors
            .AsNoTracking()
            .Include(i => i.Courses.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Enrollments.Where(e => !e.IsDeleted))
            .Include(i => i.User)
            .SingleOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (instructor == null)
            return NotFound(new { message = $"Instructor with ID {id} not found." });

        var dto = new InstructorDetailDto
        {
            Id = instructor.Id,
            EmployeeNumber = instructor.EmployeeNumber,
            FirstName = instructor.FirstName,
            LastName = instructor.LastName,
            Email = instructor.Email,
            Department = instructor.Department,
            HireDate = instructor.HireDate,
            Status = string.IsNullOrEmpty(instructor.Status) ? "Active" : instructor.Status,
            PhoneNumber = instructor.User?.PhoneNumber,
            Bio = instructor.User?.Bio,
            Rating = 4.85m,
            Avatar = !string.IsNullOrEmpty(instructor.User?.ProfilePictureUrl)
                ? instructor.User.ProfilePictureUrl
                : "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80",
            CoursesCount = instructor.Courses.Count,
            StudentsCount = instructor.Courses
                .SelectMany(c => c.Enrollments)
                .Select(e => e.StudentId)
                .Distinct()
                .Count(),
            CourseTitles = instructor.Courses.Select(c => c.Title).ToList(),
            Courses = instructor.Courses.Select(c => new InstructorCourseDto
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Category = c.Category,
                Level = c.Level,
                Status = c.Status,
                EnrolledStudentsCount = c.Enrollments.Count(e => e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed),
                LessonsCount = c.LessonsCount,
                Rating = c.Rating,
                ImageUrl = c.ImageUrl,
                StartDate = c.StartDate,
                EndDate = c.EndDate
            }).ToList()
        };

        return Ok(dto);
    }

    [Authorize]
    [HttpGet("{id:int}/schedules")]
    public async Task<IActionResult> GetInstructorSchedules(
        int id,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        var exists = await _context.Instructors.AnyAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
        if (!exists)
            return NotFound(new { message = $"Instructor with ID {id} not found." });

        var schedules = await _scheduleService.GetInstructorSchedulesAsync(id, month, year, date, cancellationToken);
        return Ok(schedules);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateInstructor([FromBody] CreateInstructorDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new { message = "First name, last name, and email are required." });
        }

        var instructor = _mapper.Map<Instructor>(dto);
        await _context.Instructors.AddAsync(instructor, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Check if existing user with same email exists to associate
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);
        if (existingUser != null)
        {
            existingUser.InstructorId = instructor.Id;
            instructor.UserId = existingUser.Id;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var resultDto = _mapper.Map<InstructorDto>(instructor);
        resultDto.Avatar = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80";
        return CreatedAtAction(nameof(GetInstructorById), new { version = "1.0", id = instructor.Id }, resultDto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/suspend")]
    public async Task<IActionResult> SuspendInstructor(
        int id,
        [FromBody] SuspendInstructorRequestDto request,
        CancellationToken cancellationToken)
    {
        var instructor = await _context.Instructors
            .Include(i => i.Courses.Where(c => !c.IsDeleted))
            .SingleOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (instructor == null)
            return NotFound(new { message = $"Instructor with ID {id} not found." });

        if (string.Equals(instructor.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Instructor is already suspended." });

        var activeCourses = instructor.Courses.Where(c => !c.IsDeleted).ToList();

        if (activeCourses.Count > 0)
        {
            var reassignmentMap = request.CourseReassignments?.ToDictionary(r => r.CourseId, r => r.ReplacementInstructorId)
                ?? new Dictionary<int, int>();

            foreach (var course in activeCourses)
            {
                if (!reassignmentMap.TryGetValue(course.Id, out var replacementId) || replacementId <= 0)
                {
                    return BadRequest(new { message = $"Course '{course.Title}' must be reassigned to a replacement instructor." });
                }

                if (replacementId == id)
                {
                    return BadRequest(new { message = $"Cannot reassign course '{course.Title}' to the instructor being suspended." });
                }

                var replacementExists = await _context.Instructors
                    .AnyAsync(i => i.Id == replacementId && !i.IsDeleted && i.Status != "Suspended", cancellationToken);

                if (!replacementExists)
                {
                    return BadRequest(new { message = $"Replacement instructor for course '{course.Title}' was not found or is suspended." });
                }
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (activeCourses.Count > 0 && request.CourseReassignments != null)
            {
                var reassignmentMap = request.CourseReassignments.ToDictionary(r => r.CourseId, r => r.ReplacementInstructorId);

                foreach (var course in activeCourses)
                {
                    var replacementId = reassignmentMap[course.Id];
                    var history = new InstructorSuspensionHistory
                    {
                        OriginalInstructorId = id,
                        ReplacementInstructorId = replacementId,
                        CourseId = course.Id,
                        SuspendedAt = DateTime.UtcNow,
                        IsRestored = false,
                        Reason = request.Reason ?? "Account suspended by administrator"
                    };

                    await _context.InstructorSuspensionHistories.AddAsync(history, cancellationToken);
                    course.InstructorId = replacementId;
                }
            }

            instructor.Status = "Suspended";

            if (!string.IsNullOrEmpty(instructor.UserId))
            {
                var user = await _userManager.FindByIdAsync(instructor.UserId);
                if (user != null)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Ok(new { message = "Instructor suspended successfully and courses reassigned." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, new { message = "Failed to suspend instructor.", details = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> ActivateInstructor(int id, CancellationToken cancellationToken)
    {
        var instructor = await _context.Instructors
            .SingleOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (instructor == null)
            return NotFound(new { message = $"Instructor with ID {id} not found." });

        if (!string.Equals(instructor.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Instructor is not currently suspended." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var unrestoredHistories = await _context.InstructorSuspensionHistories
                .Include(h => h.Course)
                .Where(h => h.OriginalInstructorId == id && !h.IsRestored)
                .ToListAsync(cancellationToken);

            foreach (var history in unrestoredHistories)
            {
                if (history.Course != null && history.Course.InstructorId == history.ReplacementInstructorId)
                {
                    history.Course.InstructorId = id;
                }
                history.IsRestored = true;
                history.RestoredAt = DateTime.UtcNow;
            }

            instructor.Status = "Active";

            if (!string.IsNullOrEmpty(instructor.UserId))
            {
                var user = await _userManager.FindByIdAsync(instructor.UserId);
                if (user != null)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Ok(new { message = "Instructor reactivated successfully and eligible courses restored." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, new { message = "Failed to reactivate instructor.", details = ex.Message });
        }
    }
}
