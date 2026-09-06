using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Grading;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class StudentService(
    TmsDbContext _context,
    IMapper _mapper,
    IWebHostEnvironment? _env = null,
    GradingService? _gradingService = null,
    IHubContext<TmsHub, ITmsClient>? _hubContext = null,
    UserManager<TmsUser>? _userManager = null) : IStudentService
{

    public async Task<PagedResponse<StudentDto>> GetStudentsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Students
            .AsNoTracking()
            .Include(s => s.Enrollments)
            .Include(s => s.User);

        IQueryable<Student> filteredQuery = query;
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            filteredQuery = filteredQuery.Where(s =>
                s.FirstName.ToLower().Contains(search) ||
                s.LastName.ToLower().Contains(search) ||
                s.RegistrationNumber.ToLower().Contains(search) ||
                s.Email.ToLower().Contains(search));
        }

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        filteredQuery = request.SortBy?.ToLower() switch
        {
            "name" or "fullname" => request.SortDescending
                ? filteredQuery.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName)
                : filteredQuery.OrderBy(s => s.FirstName).ThenBy(s => s.LastName),
            "firstname" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.FirstName) : filteredQuery.OrderBy(s => s.FirstName),
            "lastname" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.LastName) : filteredQuery.OrderBy(s => s.LastName),
            "studentid" or "registrationnumber" or "regnumber" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.RegistrationNumber) : filteredQuery.OrderBy(s => s.RegistrationNumber),
            "email" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.Email) : filteredQuery.OrderBy(s => s.Email),
            "courses" or "coursesenrolled" or "enrolledcourses" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.Enrollments.Count) : filteredQuery.OrderBy(s => s.Enrollments.Count),
            "date" or "enrollmentdate" or "registrationdate" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.EnrollmentDate) : filteredQuery.OrderBy(s => s.EnrollmentDate),
            "gpa" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.Gpa) : filteredQuery.OrderBy(s => s.Gpa),
            _ => request.SortDescending ? filteredQuery.OrderByDescending(s => s.Id) : filteredQuery.OrderBy(s => s.Id)
        };

        var items = await filteredQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(s =>
        {
            var dto = _mapper.Map<StudentDto>(s);
            dto.CoursesEnrolled = s.Enrollments.Count;
            dto.Status = !string.IsNullOrWhiteSpace(s.Status) ? s.Status : "Active";
            dto.Avatar = !string.IsNullOrEmpty(s.User?.ProfilePictureUrl)
                ? s.User.ProfilePictureUrl
                : "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80";
            return dto;
        }).ToList();

        return new PagedResponse<StudentDto>(dtos, request.Page, request.PageSize, totalCount);
    }

    public async Task<StudentDto?> GetStudentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        return student == null ? null : _mapper.Map<StudentDto>(student);
    }

    public async Task<AdminStudentDetailDto?> GetStudentDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Instructor)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Modules)
                        .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (student == null) return null;

        var enrolledCourses = student.Enrollments.Where(e => !e.IsDeleted).Select(e =>
        {
            var c = e.Course;
            var lessonsCount = c?.Modules?.SelectMany(m => m.Lessons).Count() ?? c?.LessonsCount ?? 0;
            var instructorName = c?.Instructor != null ? $"{c.Instructor.FirstName} {c.Instructor.LastName}".Trim() : null;

            return new AdminStudentEnrolledCourseDto
            {
                EnrollmentId = e.Id,
                CourseId = e.CourseId,
                Code = c?.Code ?? string.Empty,
                Title = c?.Title ?? string.Empty,
                Category = c?.Category ?? string.Empty,
                Level = c?.Level ?? string.Empty,
                ImageUrl = c?.ImageUrl,
                DurationHours = c?.DurationHours ?? 0,
                LessonsCount = lessonsCount,
                StartDate = c?.StartDate ?? DateTime.MinValue,
                EndDate = c?.EndDate ?? DateTime.MinValue,
                EnrollmentStatus = e.Status.ToString(),
                EnrolledAt = e.EnrolledAt,
                ProgressPercentage = e.Status == EnrollmentStatus.Completed ? 100m : 50m,
                FinalScore = e.Grade,
                InstructorName = instructorName
            };
        }).ToList();

        return new AdminStudentDetailDto
        {
            Id = student.Id,
            RegistrationNumber = student.RegistrationNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            PhoneNumber = student.PhoneNumber ?? student.User?.PhoneNumber,
            Department = student.Department ?? student.User?.Department,
            Bio = student.Bio ?? student.User?.Bio,
            EnrollmentDate = student.EnrollmentDate,
            Gpa = student.Gpa,
            Status = student.Status,
            Avatar = student.User?.ProfilePictureUrl ?? student.ProfilePictureUrl ?? string.Empty,
            EnrolledCourses = enrolledCourses
        };
    }

    public async Task<StudentStatsDto> GetStudentStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalStudents = await _context.Students.CountAsync(s => !s.IsDeleted, cancellationToken);
        var activeEnrolled = await _context.Enrollments
            .Where(e => e.Status == EnrollmentStatus.Active && !e.IsDeleted)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);
        var certificatesIssued = await _context.Certificates.CountAsync(cancellationToken);

        return new StudentStatsDto
        {
            TotalStudents = totalStudents,
            ActiveEnrolled = activeEnrolled,
            CertificatesIssued = certificatesIssued
        };
    }

    public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto, CancellationToken cancellationToken = default)
    {
        var student = _mapper.Map<Student>(dto);
        student.EnrollmentDate = DateTime.UtcNow;

        await _context.Students.AddAsync(student, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<StudentDto>(student);
    }

    public async Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.FindAsync(new object[] { id }, cancellationToken);
        if (student == null) return false;

        _mapper.Map(dto, student);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.FindAsync(new object[] { id }, cancellationToken);
        if (student == null || student.IsDeleted) return false;

        student.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<StudentProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return null;

        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(s => s.UserId == userId || (user.StudentId.HasValue && s.Id == user.StudentId.Value), cancellationToken);

        var regNum = !string.IsNullOrWhiteSpace(student?.RegistrationNumber)
            ? student.RegistrationNumber
            : (student != null ? $"STU-{student.Id:D5}" : $"STU-{user.Id[..Math.Min(6, user.Id.Length)].ToUpper()}");

        var enrollments = student?.Enrollments
            .Where(e => !e.IsDeleted && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected)
            .ToList() ?? new List<Enrollment>();

        var totalEnrollments = enrollments.Count;
        var activeCourses = enrollments.Count(e => e.Status == EnrollmentStatus.Active);
        var completedCourses = enrollments.Count(e => e.Status == EnrollmentStatus.Completed);

        var earnedCertificates = student != null
            ? await _context.Certificates.AsNoTracking().CountAsync(c => c.StudentId == student.Id, cancellationToken)
            : 0;

        var gradedEnrollments = enrollments.Where(e => e.Grade.HasValue && e.Course != null).ToList();
        decimal gpa = 0.0m;
        var gradingService = _gradingService ?? new GradingService();
        if (gradedEnrollments.Any())
        {
            var courseTuples = gradedEnrollments.Select(e => (e.Grade!.Value, e.Course?.Credits > 0 ? e.Course.Credits : 3));
            gpa = gradingService.CalculateEnrollmentGPA(courseTuples);
        }
        else if (student != null && student.Gpa > 0)
        {
            gpa = gradingService.CalculateGradePoint(student.Gpa);
        }

        var roles = new List<string>();
        if (_userManager != null)
        {
            var r = await _userManager.GetRolesAsync(user);
            roles.AddRange(r);
        }
        else
        {
            roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToListAsync(cancellationToken);
        }
        if (!roles.Any()) roles.Add("Student");

        return new StudentProfileDto
        {
            Id = student?.Id ?? 0,
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? student?.PhoneNumber,
            Department = user.Department ?? student?.Department ?? "General Academic Program",
            Bio = user.Bio ?? student?.Bio,
            AvatarUrl = user.ProfilePictureUrl ?? student?.ProfilePictureUrl,
            ProfilePictureUrl = user.ProfilePictureUrl ?? student?.ProfilePictureUrl,
            RegistrationNumber = regNum,
            EnrollmentDate = student?.EnrollmentDate ?? user.CreatedAt,
            Roles = roles,
            TotalEnrollments = totalEnrollments,
            ActiveCourses = activeCourses,
            CompletedCourses = completedCourses,
            EarnedCertificates = earnedCertificates,
            Gpa = gpa
        };
    }

    public async Task<bool> UpdateProfileAsync(string userId, UpdateStudentProfileDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return false;

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(dto.Department)) user.Department = dto.Department;
        user.Bio = dto.Bio;

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId || (user.StudentId.HasValue && s.Id == user.StudentId.Value), cancellationToken);

        if (student != null)
        {
            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(dto.Department)) student.Department = dto.Department;
            student.Bio = dto.Bio;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> UploadAvatarAsync(string userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null || _env == null) return null;

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "avatars");
        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"student_{user.Id}_{DateTime.UtcNow.Ticks}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var avatarUrl = $"/uploads/avatars/{fileName}";
        user.ProfilePictureUrl = avatarUrl;

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId || (user.StudentId.HasValue && s.Id == user.StudentId.Value), cancellationToken);

        if (student != null)
        {
            student.ProfilePictureUrl = avatarUrl;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return avatarUrl;
    }

    public async Task<object?> GetProgressSummaryAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        var validEnrollments = enrollments.Where(e => e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected).ToList();
        var active = validEnrollments.Count(e => e.Status == EnrollmentStatus.Active);
        var completed = validEnrollments.Count(e => e.Status == EnrollmentStatus.Completed);
        var dropped = enrollments.Count(e => e.Status == EnrollmentStatus.Dropped);

        var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        var gradedEnrollments = validEnrollments.Where(e => e.Grade.HasValue && e.Course != null).ToList();
        decimal gpa = 0.0m;
        var gradingService = _gradingService ?? new GradingService();
        if (gradedEnrollments.Any())
        {
            var courseTuples = gradedEnrollments.Select(e => (e.Grade!.Value, e.Course?.Credits > 0 ? e.Course.Credits : 3));
            gpa = gradingService.CalculateEnrollmentGPA(courseTuples);
        }
        else if (student != null && student.Gpa > 0)
        {
            gpa = gradingService.CalculateGradePoint(student.Gpa);
        }

        return new
        {
            studentId,
            gpa,
            totalEnrolled = validEnrollments.Count,
            activeCount = active,
            completedCount = completed,
            droppedCount = dropped,
            courses = validEnrollments.Select(e => new
            {
                courseId = e.CourseId,
                courseCode = e.Course?.Code ?? "",
                courseTitle = e.Course?.Title ?? "",
                status = e.Status.ToString(),
                grade = e.Grade
            }).ToList()
        };
    }

    public async Task<object?> GetCourseProgressDetailAsync(int studentId, int courseId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
                    .ThenInclude(i => i.User)
            .Include(e => e.Course)
                .ThenInclude(c => c.Schedules)
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId && !e.IsDeleted, cancellationToken);

        if (enrollment == null || enrollment.Course == null || enrollment.Course.IsDeleted) return null;

        var course = enrollment.Course;

        var attendances = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.CourseId == courseId)
            .OrderByDescending(a => a.SessionDate)
            .ToListAsync(cancellationToken);

        int presentCount = attendances.Count(a => a.Status == AttendanceStatus.Present);
        int lateCount = attendances.Count(a => a.Status == AttendanceStatus.Late);
        int absentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        int totalSessions = attendances.Count;
        decimal attendanceRate = totalSessions > 0
            ? Math.Round(((decimal)(presentCount + lateCount) / totalSessions) * 100m, 1)
            : 0m;

        var attendanceSessions = attendances.Select(a => new AttendanceSessionDto
        {
            Id = a.Id,
            SessionDate = a.SessionDate,
            Status = a.Status.ToString(),
            Remarks = a.Remarks
        }).ToList();

        var assignments = await _context.Assignments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && !a.IsDeleted)
            .OrderBy(a => a.AssignmentNumber)
            .ThenBy(a => a.DueDate)
            .ToListAsync(cancellationToken);

        var studentAssessments = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.CourseId == courseId)
            .ToListAsync(cancellationToken);

        var inProgressAssignments = new List<InProgressAssignmentDto>();
        var completedAssignments = new List<CompletedAssignmentDto>();

        foreach (var a in assignments)
        {
            var ass = studentAssessments.FirstOrDefault(s => s.AssignmentId == a.Id);
            if (ass != null && (string.Equals(ass.Status, "Graded", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(ass.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                               ass.ScoredPoints.HasValue))
            {
                completedAssignments.Add(new CompletedAssignmentDto
                {
                    AssignmentId = a.Id,
                    AssignmentNumber = a.AssignmentNumber,
                    Title = a.Title,
                    DueDate = a.DueDate,
                    MaxPoints = a.MaxPoints,
                    ScoredPoints = ass.ScoredPoints ?? 0,
                    GradedAt = ass.GradedAt ?? ass.SubmittedAt,
                    Feedback = ass.Feedback
                });
            }
            else
            {
                bool isSubmitted = ass != null && (string.Equals(ass.Status, "Submitted", StringComparison.OrdinalIgnoreCase) || ass.SubmittedAt.HasValue);
                inProgressAssignments.Add(new InProgressAssignmentDto
                {
                    AssignmentId = a.Id,
                    AssignmentNumber = a.AssignmentNumber,
                    Title = a.Title,
                    DueDate = a.DueDate,
                    MaxPoints = a.MaxPoints,
                    SubmissionStatus = isSubmitted ? "Submitted" : "Not Submitted",
                    SubmittedAt = ass?.SubmittedAt
                });
            }
        }

        var gradingService = new TmsApi.Application.Grading.GradingService();
        var finalGrade = new FinalGradeDto
        {
            IsEvaluated = enrollment.Grade.HasValue,
            Status = enrollment.Grade.HasValue ? "Evaluated" : "Pending",
            Score = enrollment.Grade,
            Letter = enrollment.Grade.HasValue ? gradingService.CalculateGradeLevel(enrollment.Grade.Value).ToString() : null,
            GradePoint = enrollment.Grade.HasValue ? gradingService.CalculateGradePoint(enrollment.Grade.Value) : null
        };

        var schedules = (course.Schedules ?? new List<CourseSchedule>())
            .Where(s => !s.IsDeleted)
            .Select(s => new CourseScheduleItemDto
            {
                Id = s.Id,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Location = s.Location ?? "",
                Room = s.Room ?? "",
                ScheduleType = s.ScheduleType
            }).ToList();

        int totalAssessments = assignments.Count;
        int completedCount = completedAssignments.Count;
        decimal progressPercentage = totalAssessments > 0
            ? Math.Round(((decimal)completedCount / totalAssessments) * 100m, 1)
            : (enrollment.Status == EnrollmentStatus.Completed ? 100m : 0m);

        return new StudentCourseProgressDetailDto
        {
            CourseSummary = new CourseSummaryDto
            {
                CourseId = course.Id,
                CourseCode = course.Code,
                CourseTitle = course.Title,
                Description = course.Description ?? "",
                Department = course.Department,
                Level = course.Level.ToString(),
                Category = course.Category,
                Credits = course.Credits,
                StartDate = course.StartDate,
                EndDate = course.EndDate,
                EnrollmentStatus = enrollment.Status.ToString(),
                InstructorName = course.Instructor != null ? $"{course.Instructor.FirstName} {course.Instructor.LastName}".Trim() : "Not Assigned",
                InstructorEmail = course.Instructor?.User?.Email ?? course.Instructor?.Email ?? ""
            },
            Progress = new CourseProgressSummaryDto
            {
                ProgressPercentage = progressPercentage,
                CompletedAssessments = completedCount,
                TotalAssessments = totalAssessments,
                Status = enrollment.Status.ToString()
            },
            Attendance = new AttendanceSummaryDto
            {
                PresentCount = presentCount,
                LateCount = lateCount,
                AbsentCount = absentCount,
                TotalSessions = totalSessions,
                AttendanceRate = attendanceRate,
                Sessions = attendanceSessions
            },
            Assignments = new AssignmentSectionsDto
            {
                InProgress = inProgressAssignments,
                Completed = completedAssignments
            },
            FinalGrade = finalGrade,
            Schedules = schedules
        };
    }

    public async Task<bool> WithdrawFromCourseAsync(int studentId, int courseId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId && !e.IsDeleted, cancellationToken);

        if (enrollment == null || enrollment.Course == null) return false;

        var course = enrollment.Course;
        if (course.StartDate.Date <= DateTime.UtcNow.Date) return false;

        enrollment.Status = EnrollmentStatus.Dropped;
        await _context.SaveChangesAsync(cancellationToken);

        var enrolledCount = await _context.Enrollments
            .CountAsync(e => e.CourseId == courseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Pending) && !e.IsDeleted, cancellationToken);

        course.EnrolledStudentsCount = enrolledCount;
        await _context.SaveChangesAsync(cancellationToken);

        if (_hubContext != null)
        {
            await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "withdrawn", studentId, courseId });
            await _hubContext.Clients.All.ReceiveEnrollmentNotification(new { action = "withdrawn", studentId, courseId });
            await _hubContext.Clients.All.ReceiveCapacityAlert(new
            {
                courseId = course.Id,
                enrolledCount,
                maxCapacity = course.MaxCapacity
            });
        }

        return true;
    }
}
