using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;

    public ScheduleService(TmsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ScheduleDto>> GetSchedulesAsync(
        string? category = null,
        int? month = null,
        int? year = null,
        int? studentId = null,
        int? instructorId = null,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        var monthStart = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = new DateTime(targetYear, targetMonth, daysInMonth, 23, 59, 59, DateTimeKind.Utc);

        var resultList = new List<ScheduleDto>();

        HashSet<int>? studentEnrolledCourseIds = null;
        if (studentId.HasValue)
        {
            studentEnrolledCourseIds = (await _context.Enrollments
                .Where(e => e.StudentId == studentId.Value && !e.IsDeleted && e.Status == EnrollmentStatus.Active)
                .Select(e => e.CourseId)
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        string? instructorDepartment = null;
        if (instructorId.HasValue)
        {
            instructorDepartment = await _context.Instructors
                .Where(i => i.Id == instructorId.Value && !i.IsDeleted)
                .Select(i => i.Department)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // 1. One-off explicit schedules in the Schedule table
        var oneOffQuery = _context.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
                .ThenInclude(c => c!.Instructor)
                    .ThenInclude(i => i!.User)
            .Where(s => !s.IsDeleted);

        if (year.HasValue && month.HasValue)
        {
            oneOffQuery = oneOffQuery.Where(s => s.Date.Year == targetYear && s.Date.Month == targetMonth);
        }

        var explicitSchedules = await oneOffQuery.ToListAsync(cancellationToken);
        foreach (var es in explicitSchedules)
        {
            bool isVisible = false;
            var audience = string.IsNullOrWhiteSpace(es.TargetAudience) ? "All" : es.TargetAudience;

            if (isAdmin)
            {
                isVisible = true;
            }
            else if (string.Equals(audience, "All", StringComparison.OrdinalIgnoreCase))
            {
                isVisible = true;
            }
            else if (studentId.HasValue)
            {
                if (string.Equals(audience, "AllStudents", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(audience, "Students", StringComparison.OrdinalIgnoreCase))
                {
                    isVisible = true;
                }
                else if (string.Equals(audience, "CourseStudents", StringComparison.OrdinalIgnoreCase))
                {
                    isVisible = es.TargetCourseId.HasValue &&
                                studentEnrolledCourseIds != null &&
                                studentEnrolledCourseIds.Contains(es.TargetCourseId.Value);
                }
            }
            else if (instructorId.HasValue)
            {
                if (string.Equals(audience, "AllInstructors", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(audience, "Instructors", StringComparison.OrdinalIgnoreCase))
                {
                    isVisible = true;
                }
                else if (string.Equals(audience, "DepartmentInstructors", StringComparison.OrdinalIgnoreCase))
                {
                    isVisible = !string.IsNullOrEmpty(es.TargetDepartment) &&
                                !string.IsNullOrEmpty(instructorDepartment) &&
                                string.Equals(es.TargetDepartment, instructorDepartment, StringComparison.OrdinalIgnoreCase);
                }
            }

            if (!isVisible) continue;

            var instructor = es.Course?.Instructor;
            var instructorName = instructor != null ? $"{instructor.FirstName} {instructor.LastName}" : (!string.IsNullOrEmpty(es.PicName) ? es.PicName : "Instructor");
            var instructorAvatar = instructor?.User?.ProfilePictureUrl ?? es.PicAvatar;

            resultList.Add(new ScheduleDto
            {
                Id = es.Id,
                CourseId = es.CourseId,
                CourseTitle = es.Course?.Title ?? es.Title,
                CourseCode = es.Course?.Code ?? "TMS",
                Title = es.Title,
                Category = es.Category,
                Date = es.Date,
                StartTime = es.StartTime,
                EndTime = es.EndTime,
                Location = !string.IsNullOrWhiteSpace(es.Location) ? es.Location : "Main Campus",
                Room = !string.IsNullOrWhiteSpace(es.Location) ? es.Location : "Room 101",
                ScheduleType = es.ScheduleType,
                TargetAudience = es.TargetAudience,
                TargetCourseId = es.TargetCourseId,
                TargetDepartment = es.TargetDepartment,
                InstructorName = instructorName,
                InstructorAvatar = instructorAvatar,
                PicName = instructorName,
                PicAvatar = instructorAvatar,
                Notes = es.Notes,
                AttendeesCount = es.Course != null ? es.Course.EnrolledStudentsCount : es.AttendeesCount,
                CreatedAt = es.CreatedAt
            });
        }

        // 2. Recurring CourseSchedules bounded strictly by Course [StartDate, EndDate]
        var courses = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
                .ThenInclude(i => i!.User)
            .Include(c => c.Schedules)
            .Include(c => c.Enrollments)
            .Where(c => !c.IsDeleted && c.StartDate <= monthEnd && c.EndDate >= monthStart)
            .ToListAsync(cancellationToken);

        foreach (var course in courses)
        {
            if (course.Schedules == null || !course.Schedules.Any()) continue;
            var enrolledCount = course.Enrollments.Count(e => (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted);
            var instructorName = course.Instructor != null ? $"{course.Instructor.FirstName} {course.Instructor.LastName}" : "Faculty Instructor";
            var instructorAvatar = course.Instructor?.User?.ProfilePictureUrl ?? "";

            for (int day = 1; day <= daysInMonth; day++)
            {
                var sessionDate = new DateTime(targetYear, targetMonth, day, 0, 0, 0, DateTimeKind.Utc);
                if (sessionDate.Date < course.StartDate.Date || sessionDate.Date > course.EndDate.Date)
                    continue;

                foreach (var cs in course.Schedules.Where(s => !s.IsDeleted && s.DayOfWeek == sessionDate.DayOfWeek))
                {
                    // Avoid duplicate if explicit one-off already exists for this course on this day and start time
                    if (resultList.Any(r => r.CourseId == course.Id && r.Date.Date == sessionDate.Date && r.StartTime == cs.StartTime))
                        continue;

                    resultList.Add(new ScheduleDto
                    {
                        Id = Math.Abs(HashCode.Combine(cs.Id, targetYear, targetMonth, day)),
                        CourseId = course.Id,
                        CourseTitle = course.Title,
                        CourseCode = course.Code,
                        Title = course.Title,
                        Category = course.Category,
                        Date = sessionDate,
                        StartTime = cs.StartTime,
                        EndTime = cs.EndTime,
                        Location = string.IsNullOrWhiteSpace(cs.Location) ? "Main Training Center" : cs.Location,
                        Room = string.IsNullOrWhiteSpace(cs.Room) ? "Room 204" : cs.Room,
                        ScheduleType = string.IsNullOrWhiteSpace(cs.ScheduleType) ? "Lecture" : cs.ScheduleType,
                        InstructorName = instructorName,
                        InstructorAvatar = instructorAvatar,
                        PicName = instructorName,
                        PicAvatar = instructorAvatar,
                        Notes = $"Scheduled academic session for {course.Code}. {cs.Room} ({cs.Location}).",
                        AttendeesCount = enrolledCount > 0 ? enrolledCount : course.EnrolledStudentsCount,
                        CreatedAt = course.StartDate,
                        CourseStatus = course.Status
                    });
                }
            }
        }

        var finalQuery = resultList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            finalQuery = finalQuery.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                                               s.ScheduleType.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        return finalQuery.OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToList();
    }

    public async Task<IEnumerable<ScheduleDto>> GetStudentSchedulesAsync(int studentId, string? view = null, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        // Find courses student is enrolled in
        var enrolledCourseIds = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .Select(e => e.CourseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = _context.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
                .ThenInclude(c => c!.Instructor)
                    .ThenInclude(i => i!.User)
            .Where(s => !s.IsDeleted);

        // Strictly constrain schedules to enrolled courses
        query = query.Where(s => s.CourseId.HasValue && enrolledCourseIds.Contains(s.CourseId.Value));

        var refDate = date ?? DateTime.UtcNow;

        if (string.Equals(view, "day", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Date.Date == refDate.Date);
        }
        else if (string.Equals(view, "week", StringComparison.OrdinalIgnoreCase))
        {
            var startOfWeek = refDate.AddDays(-(int)refDate.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);
            query = query.Where(s => s.Date >= startOfWeek && s.Date < endOfWeek);
        }
        else if (string.Equals(view, "month", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Date.Year == refDate.Year && s.Date.Month == refDate.Month);
        }

        var schedules = await query.OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ScheduleDto>>(schedules);
    }

    public async Task<StudentScheduleSummaryDto> GetStudentScheduleOverviewAsync(
        int studentId,
        int? year = null,
        int? month = null,
        int? courseId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Get active or completed enrollments for this student with Course, Course.Schedules, and Course.Instructor
        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c!.Schedules)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Instructor)
                    .ThenInclude(i => i!.User)
            .Where(e => e.StudentId == studentId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        var enrolledCourseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();

        var enrolledCoursesLookup = enrollments
            .Where(e => e.Course != null && !e.Course.IsDeleted)
            .Select(e => new CourseLookupDto
            {
                Id = e.Course!.Id,
                Title = e.Course.Title,
                Code = e.Course.Code
            })
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .OrderBy(c => c.Title)
            .ToList();

        if (enrolledCourseIds.Count == 0)
        {
            return new StudentScheduleSummaryDto
            {
                ClassesThisMonth = 0,
                ClassesThisWeek = 0,
                EnrolledCoursesCount = 0,
                NextClass = null,
                EnrolledCourses = new List<CourseLookupDto>(),
                Schedules = new List<ScheduleDto>()
            };
        }

        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);

        var monthSchedules = new List<ScheduleDto>();

        // 2. Project recurring class schedules for each enrolled course within [StartDate, EndDate]
        foreach (var enrollment in enrollments)
        {
            var course = enrollment.Course;
            if (course == null || course.IsDeleted) continue;
            if (courseId.HasValue && courseId.Value > 0 && course.Id != courseId.Value) continue;

            if (course.Schedules == null || !course.Schedules.Any()) continue;
            if (course.EndDate < course.StartDate) continue;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(targetYear, targetMonth, day, 0, 0, 0, DateTimeKind.Utc);

                // Strictly bounded by Course StartDate and EndDate
                if (date.Date < course.StartDate.Date || date.Date > course.EndDate.Date)
                    continue;

                foreach (var sched in course.Schedules.Where(s => !s.IsDeleted))
                {
                    if (sched.DayOfWeek == date.DayOfWeek)
                    {
                        var instructorName = course.Instructor != null
                            ? $"{course.Instructor.FirstName} {course.Instructor.LastName}"
                            : "Faculty Instructor";

                        monthSchedules.Add(new ScheduleDto
                        {
                            Id = Math.Abs(HashCode.Combine(sched.Id, targetYear, targetMonth, day)),
                            CourseId = course.Id,
                            CourseTitle = course.Title,
                            CourseCode = course.Code,
                            Title = course.Title,
                            Category = course.Category,
                            Date = date,
                            StartTime = sched.StartTime,
                            EndTime = sched.EndTime,
                            Location = string.IsNullOrWhiteSpace(sched.Location) ? "Main Training Center" : sched.Location,
                            Room = string.IsNullOrWhiteSpace(sched.Room) ? "Room 204" : sched.Room,
                            ScheduleType = string.IsNullOrWhiteSpace(sched.ScheduleType) ? "Lecture" : sched.ScheduleType,
                            InstructorName = instructorName,
                            InstructorAvatar = course.Instructor?.User?.ProfilePictureUrl ?? "",
                            PicName = instructorName,
                            Notes = $"Regular class session for {course.Code}. Room: {sched.Room} ({sched.Location})",
                            AttendeesCount = course.EnrolledStudentsCount,
                            CreatedAt = course.StartDate,
                            CourseStatus = course.Status,
                            EnrollmentStatus = enrollment.Status.ToString()
                        });
                    }
                }
            }
        }

        // 3. Include any one-off schedules (e.g. exams, special workshops) from the Schedule table
        var oneOffQuery = _context.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
                .ThenInclude(c => c!.Instructor)
                    .ThenInclude(i => i!.User)
            .Where(s => !s.IsDeleted && s.CourseId.HasValue && enrolledCourseIds.Contains(s.CourseId.Value)
                        && s.Date.Year == targetYear && s.Date.Month == targetMonth);

        if (courseId.HasValue && courseId.Value > 0)
        {
            oneOffQuery = oneOffQuery.Where(s => s.CourseId == courseId.Value);
        }

        var oneOffSchedules = await oneOffQuery.ToListAsync(cancellationToken);
        var oneOffDtos = _mapper.Map<List<ScheduleDto>>(oneOffSchedules);
        foreach (var dto in oneOffDtos)
        {
            if (dto.CourseId.HasValue)
            {
                var en = enrollments.FirstOrDefault(e => e.CourseId == dto.CourseId.Value);
                if (en != null)
                {
                    dto.CourseStatus = en.Course?.Status ?? "Active";
                    dto.EnrollmentStatus = en.Status.ToString();
                }
            }
        }
        monthSchedules.AddRange(oneOffDtos);

        // Sort all month schedules chronologically
        monthSchedules = monthSchedules
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToList();

        // 4. Calculate ClassesThisWeek (current week relative to UtcNow)
        var now = DateTime.UtcNow;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);
        int weekClassesCount = 0;

        foreach (var enrollment in enrollments)
        {
            var course = enrollment.Course;
            if (course == null || course.IsDeleted || course.Schedules == null) continue;
            if (courseId.HasValue && courseId.Value > 0 && course.Id != courseId.Value) continue;

            for (var d = startOfWeek; d < endOfWeek; d = d.AddDays(1))
            {
                if (d.Date >= course.StartDate.Date && d.Date <= course.EndDate.Date)
                {
                    weekClassesCount += course.Schedules.Count(s => !s.IsDeleted && s.DayOfWeek == d.DayOfWeek);
                }
            }
        }

        // Add one-off schedules occurring this week
        weekClassesCount += await _context.Schedules
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.CourseId.HasValue && enrolledCourseIds.Contains(s.CourseId.Value)
                             && s.Date >= startOfWeek && s.Date < endOfWeek, cancellationToken);

        // 5. Next Class (earliest upcoming class starting from now)
        ScheduleDto? nextClassDto = null;
        for (int offset = 0; offset <= 30; offset++)
        {
            var checkDate = now.Date.AddDays(offset);
            var dayClasses = new List<ScheduleDto>();

            foreach (var enrollment in enrollments)
            {
                var course = enrollment.Course;
                if (course == null || course.IsDeleted || course.Schedules == null) continue;
                if (courseId.HasValue && courseId.Value > 0 && course.Id != courseId.Value) continue;

                if (checkDate >= course.StartDate.Date && checkDate <= course.EndDate.Date)
                {
                    foreach (var sched in course.Schedules.Where(s => !s.IsDeleted && s.DayOfWeek == checkDate.DayOfWeek))
                    {
                        var instructorName = course.Instructor != null
                            ? $"{course.Instructor.FirstName} {course.Instructor.LastName}"
                            : "Faculty Instructor";

                        dayClasses.Add(new ScheduleDto
                        {
                            CourseId = course.Id,
                            CourseTitle = course.Title,
                            CourseCode = course.Code,
                            Title = course.Title,
                            Category = course.Category,
                            Date = checkDate,
                            StartTime = sched.StartTime,
                            EndTime = sched.EndTime,
                            Location = string.IsNullOrWhiteSpace(sched.Room) ? "Main Campus" : sched.Room,
                            ScheduleType = sched.ScheduleType,
                            InstructorName = instructorName
                        });
                    }
                }
            }

            if (dayClasses.Any())
            {
                nextClassDto = dayClasses.OrderBy(s => s.StartTime).First();
                break;
            }
        }

        if (nextClassDto == null && monthSchedules.Any())
        {
            nextClassDto = monthSchedules.First();
        }

        return new StudentScheduleSummaryDto
        {
            ClassesThisMonth = monthSchedules.Count,
            ClassesThisWeek = weekClassesCount,
            EnrolledCoursesCount = enrolledCoursesLookup.Count,
            NextClass = nextClassDto,
            EnrolledCourses = enrolledCoursesLookup,
            Schedules = monthSchedules
        };
    }

    public async Task<ScheduleDto?> GetScheduleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        return schedule == null ? null : _mapper.Map<ScheduleDto>(schedule);
    }

    public async Task<IEnumerable<ScheduleDto>> GetInstructorSchedulesAsync(int instructorId, int? month = null, int? year = null, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);

        var assignedCourses = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
                .ThenInclude(i => i!.User)
            .Include(c => c.Schedules)
            .Include(c => c.Enrollments)
            .Where(c => c.InstructorId == instructorId && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var assignedCourseIds = assignedCourses.Select(c => c.Id).ToList();
        var resultList = new List<ScheduleDto>();

        // 1. One-off explicit schedules for this instructor's courses or instructor name
        var instructor = await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.Id == instructorId, cancellationToken);
        var instructorName = instructor != null ? $"{instructor.FirstName} {instructor.LastName}" : string.Empty;

        var explicitSchedules = await _context.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Where(s => !s.IsDeleted &&
                        ((s.CourseId.HasValue && assignedCourseIds.Contains(s.CourseId.Value)) ||
                         (!string.IsNullOrEmpty(instructorName) && s.PicName == instructorName)))
            .ToListAsync(cancellationToken);

        foreach (var es in explicitSchedules)
        {
            if (date.HasValue && es.Date.Date != date.Value.Date) continue;
            if (!date.HasValue && (es.Date.Year != targetYear || es.Date.Month != targetMonth)) continue;

            var course = assignedCourses.FirstOrDefault(c => c.Id == es.CourseId);
            var studentCount = course != null
                ? course.Enrollments.Count(e => e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed)
                : es.AttendeesCount;

            resultList.Add(new ScheduleDto
            {
                Id = es.Id,
                CourseId = es.CourseId,
                CourseTitle = es.Course?.Title ?? es.Title,
                CourseCode = es.Course?.Code ?? "CRS",
                Title = es.Title,
                Category = es.Category,
                Date = es.Date,
                StartTime = es.StartTime,
                EndTime = es.EndTime,
                Location = es.Location,
                Room = es.Location,
                ScheduleType = es.ScheduleType,
                InstructorName = instructorName,
                InstructorAvatar = instructor?.User?.ProfilePictureUrl ?? "",
                PicName = instructorName,
                PicAvatar = instructor?.User?.ProfilePictureUrl ?? "",
                Notes = es.Notes,
                AttendeesCount = studentCount
            });
        }

        // 2. Project recurring schedules
        foreach (var course in assignedCourses)
        {
            if (course.Schedules == null || !course.Schedules.Any()) continue;
            var studentCount = course.Enrollments.Count(e => e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed);

            int startDay = 1;
            int endDay = daysInMonth;

            if (date.HasValue)
            {
                if (date.Value.Year != targetYear || date.Value.Month != targetMonth) continue;
                startDay = date.Value.Day;
                endDay = date.Value.Day;
            }

            for (int day = startDay; day <= endDay; day++)
            {
                var sessionDate = new DateTime(targetYear, targetMonth, day, 0, 0, 0, DateTimeKind.Utc);
                if (sessionDate.Date < course.StartDate.Date || sessionDate.Date > course.EndDate.Date)
                    continue;

                foreach (var cs in course.Schedules.Where(s => !s.IsDeleted && s.DayOfWeek == sessionDate.DayOfWeek))
                {
                    // Don't duplicate if explicit schedule exists on that day and course
                    if (resultList.Any(r => r.CourseId == course.Id && r.Date.Date == sessionDate.Date && r.StartTime == cs.StartTime))
                        continue;

                    resultList.Add(new ScheduleDto
                    {
                        Id = Math.Abs(HashCode.Combine(cs.Id, targetYear, targetMonth, day)),
                        CourseId = course.Id,
                        CourseTitle = course.Title,
                        CourseCode = course.Code,
                        Title = course.Title,
                        Category = course.Category,
                        Date = sessionDate,
                        StartTime = cs.StartTime,
                        EndTime = cs.EndTime,
                        Location = string.IsNullOrWhiteSpace(cs.Location) ? "Main Training Center" : cs.Location,
                        Room = string.IsNullOrWhiteSpace(cs.Room) ? "Room 204" : cs.Room,
                        ScheduleType = string.IsNullOrWhiteSpace(cs.ScheduleType) ? "Lecture" : cs.ScheduleType,
                        InstructorName = instructorName,
                        InstructorAvatar = instructor?.User?.ProfilePictureUrl ?? "",
                        PicName = instructorName,
                        PicAvatar = instructor?.User?.ProfilePictureUrl ?? "",
                        Notes = $"Course {course.Code} training session. Room: {cs.Room}",
                        AttendeesCount = studentCount
                    });
                }
            }
        }

        return resultList.OrderBy(s => s.Date).ThenBy(s => s.StartTime);
    }

    public async Task<ScheduleDto> CreateScheduleAsync(CreateScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var schedule = _mapper.Map<Schedule>(dto);
        if (schedule.Date == default)
        {
            schedule.Date = DateTime.UtcNow;
        }

        schedule.TargetAudience = !string.IsNullOrWhiteSpace(dto.TargetAudience) ? dto.TargetAudience : "All";
        schedule.TargetCourseId = dto.TargetCourseId;
        schedule.TargetDepartment = dto.TargetDepartment;

        await _context.Schedules.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var res = _mapper.Map<ScheduleDto>(schedule);
        res.TargetAudience = schedule.TargetAudience;
        res.TargetCourseId = schedule.TargetCourseId;
        res.TargetDepartment = schedule.TargetDepartment;
        return res;
    }

    public async Task<bool> UpdateScheduleAsync(int id, UpdateScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (schedule == null) return false;

        _mapper.Map(dto, schedule);
        schedule.TargetAudience = !string.IsNullOrWhiteSpace(dto.TargetAudience) ? dto.TargetAudience : "All";
        schedule.TargetCourseId = dto.TargetCourseId;
        schedule.TargetDepartment = dto.TargetDepartment;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteScheduleAsync(int id, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (schedule == null) return false;

        schedule.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
