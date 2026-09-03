using System.Dynamic;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Shaping;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class CourseService : ICourseService
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;
    private readonly DataShaper<CourseDto> _dataShaper;

    public CourseService(TmsDbContext context, IMapper mapper, DataShaper<CourseDto> dataShaper)
    {
        _context = context;
        _mapper = mapper;
        _dataShaper = dataShaper;
    }

    public async Task<PagedResponse<CourseDto>> GetCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .Include(c => c.Schedules)
            .Where(c => !c.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(search) ||
                c.Code.ToLower().Contains(search) ||
                c.Category.ToLower().Contains(search) ||
                c.Department.ToLower().Contains(search) ||
                (c.Instructor != null && (c.Instructor.FirstName.ToLower().Contains(search) || c.Instructor.LastName.ToLower().Contains(search))));
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var dept = request.Department.Trim().ToLower();
            query = query.Where(c => c.Department.ToLower() == dept);
        }

        if (!string.IsNullOrWhiteSpace(request.Category) && request.Category != "All" && request.Category != "All Category")
        {
            var cat = request.Category.Trim().ToLower();
            query = query.Where(c => c.Category.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "All")
        {
            var st = request.Status.Trim().ToLower();
            query = query.Where(c => c.Status.ToLower() == st);
        }

        if (!string.IsNullOrWhiteSpace(request.SkillLevel) && request.SkillLevel != "All" && request.SkillLevel != "All Levels")
        {
            var lvl = request.SkillLevel.Trim().ToLower();
            query = query.Where(c => c.Level.ToLower() == lvl);
        }

        if (!string.IsNullOrWhiteSpace(request.SubjectMatter) && request.SubjectMatter != "All" && request.SubjectMatter != "All Subjects")
        {
            var subj = request.SubjectMatter.Trim().ToLower();
            query = query.Where(c => c.Department.ToLower() == subj || c.Category.ToLower() == subj);
        }

        if (!string.IsNullOrWhiteSpace(request.Availability) && request.Availability != "All")
        {
            var avail = request.Availability.Trim().ToLower();
            if (avail == "available")
            {
                query = query.Where(c => c.Enrollments.Count(e => (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Pending) && !e.IsDeleted) < c.MaxCapacity);
            }
            else if (avail == "full")
            {
                query = query.Where(c => c.Enrollments.Count(e => (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Pending) && !e.IsDeleted) >= c.MaxCapacity);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDescending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
            "title" => request.SortDescending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            "rating" => request.SortDescending ? query.OrderByDescending(c => c.Rating) : query.OrderBy(c => c.Rating),
            "maxcapacity" => request.SortDescending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
            _ => query.OrderBy(c => c.Id)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<CourseDto>(_mapper.Map<IEnumerable<CourseDto>>(items), request.Page, request.PageSize, totalCount);
    }

    public async Task<PagedResponse<ExpandoObject>> GetShapedCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var response = await GetCoursesAsync(request, cancellationToken);
        var shapedItems = _dataShaper.ShapeData(response.Items, request.Fields);
        return new PagedResponse<ExpandoObject>(shapedItems, response.Page, response.PageSize, response.TotalCount);
    }

    public async Task<CourseDto?> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .Include(c => c.Schedules)
            .Include(c => c.Modules.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
            .Include(c => c.Resources)
            .Where(c => !c.IsDeleted)
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        return course == null ? null : _mapper.Map<CourseDto>(course);
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var course = _mapper.Map<Course>(dto);
        await _context.Courses.AddAsync(course, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CourseDto>(course);
    }

    public async Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.FindAsync(new object[] { id }, cancellationToken);
        if (course == null || course.IsDeleted) return false;

        _mapper.Map(dto, course);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.FindAsync(new object[] { id }, cancellationToken);
        if (course == null || course.IsDeleted) return false;

        course.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<StudentEnrolledCourseDto>> GetStudentEnrolledCoursesAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
                    .ThenInclude(i => i.User)
            .Include(e => e.Course)
                .ThenInclude(c => c.Schedules)
            .Include(e => e.Course)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .Where(e => e.StudentId == studentId && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected && !e.IsDeleted && !e.IsArchived && e.Course != null && !e.Course.IsDeleted)
            .OrderByDescending(e => e.EnrolledAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var studentAssessments = await _context.Assessments
            .Where(a => a.StudentId == studentId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var result = new List<StudentEnrolledCourseDto>();

        foreach (var en in enrollments)
        {
            var course = en.Course!;
            var courseAssessments = studentAssessments.Where(a => a.CourseId == en.CourseId).ToList();

            int totalLessons = course.Modules.SelectMany(m => m.Lessons).Count();
            if (totalLessons == 0) totalLessons = course.LessonsCount;

            int totalModules = course.Modules.Count;
            int totalAssessments = courseAssessments.Count;
            int completedAssessments = courseAssessments.Count(a => a.Status == "Graded" || a.Status == "Submitted");

            decimal progressPct = en.Status == EnrollmentStatus.Completed
                ? 100m
                : totalAssessments > 0
                    ? Math.Min(100m, Math.Round((completedAssessments / (decimal)totalAssessments) * 100m, 1))
                    : 0m;

            int completedLessons = en.Status == EnrollmentStatus.Completed
                ? totalLessons
                : totalLessons > 0 ? Math.Min(totalLessons, (int)Math.Round((progressPct / 100m) * totalLessons)) : 0;

            int completedModules = en.Status == EnrollmentStatus.Completed
                ? totalModules
                : totalModules > 0 ? Math.Min(totalModules, (int)Math.Round((progressPct / 100m) * totalModules)) : 0;

            string academicStatus;
            if (en.Status == EnrollmentStatus.Completed || (course.EndDate.Date < now.Date && en.Grade.HasValue))
            {
                academicStatus = "Completed";
            }
            else if (course.StartDate.Date > now.Date)
            {
                academicStatus = "Upcoming";
            }
            else
            {
                academicStatus = "Active";
            }

            var activeSchedules = course.Schedules.Where(s => !s.IsDeleted).ToList();
            var schedulesDto = _mapper.Map<List<CourseScheduleDto>>(activeSchedules);

            string daysDisplay = string.Empty;
            string timeDisplay = string.Empty;
            string location = "Main Training Center";
            string room = "Room 204";

            if (activeSchedules.Any())
            {
                var dayNames = activeSchedules.Select(s => s.DayOfWeek switch
                {
                    DayOfWeek.Monday => "Mon",
                    DayOfWeek.Tuesday => "Tue",
                    DayOfWeek.Wednesday => "Wed",
                    DayOfWeek.Thursday => "Thu",
                    DayOfWeek.Friday => "Fri",
                    DayOfWeek.Saturday => "Sat",
                    DayOfWeek.Sunday => "Sun",
                    _ => s.DayOfWeek.ToString()
                }).Distinct();
                daysDisplay = string.Join(" \u2022 ", dayNames);

                var firstSched = activeSchedules.First();
                timeDisplay = $"{firstSched.StartTime} \u2013 {firstSched.EndTime}";
                if (!string.IsNullOrWhiteSpace(firstSched.Location)) location = firstSched.Location;
                if (!string.IsNullOrWhiteSpace(firstSched.Room)) room = firstSched.Room;
            }

            var instructorName = course.Instructor != null
                ? $"{course.Instructor.FirstName} {course.Instructor.LastName}".Trim()
                : "Faculty Instructor";

            result.Add(new StudentEnrolledCourseDto
            {
                CourseId = course.Id,
                CourseCode = course.Code,
                Title = course.Title,
                Description = course.Description,
                Level = string.IsNullOrWhiteSpace(course.Level) ? "Beginner" : course.Level,
                Department = course.Department,
                Category = course.Category,
                ImageUrl = course.ImageUrl,
                Credits = course.Credits,
                DurationWeeks = course.DurationWeeks > 0 ? course.DurationWeeks : 6,
                StartDate = course.StartDate,
                EndDate = course.EndDate,
                Schedules = schedulesDto,
                DaysDisplay = daysDisplay,
                TimeDisplay = timeDisplay,
                Location = location,
                Room = room,
                InstructorId = course.InstructorId,
                InstructorName = instructorName,
                InstructorAvatar = course.Instructor?.User?.ProfilePictureUrl,
                InstructorDepartment = course.Instructor?.Department,
                EnrollmentId = en.Id,
                EnrolledAt = en.EnrolledAt,
                EnrollmentStatus = en.Status.ToString(),
                AcademicStatus = academicStatus,
                ProgressPercentage = progressPct,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                TotalModules = totalModules,
                CompletedModules = completedModules,
                Grade = en.Grade
            });
        }

        return result;
    }

    public async Task<StudentCoursesStatisticsDto> GetStudentCoursesStatisticsAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var courses = await GetStudentEnrolledCoursesAsync(studentId, cancellationToken);

        int total = courses.Count;
        int active = courses.Count(c => c.AcademicStatus == "Active");
        int completed = courses.Count(c => c.AcademicStatus == "Completed");
        int upcoming = courses.Count(c => c.AcademicStatus == "Upcoming");
        int totalCredits = courses.Where(c => c.AcademicStatus != "Withdrawn").Sum(c => c.Credits);

        decimal avgProgress = 0m;
        var validForProgress = courses.Where(c => c.AcademicStatus != "Withdrawn").ToList();
        if (validForProgress.Any())
        {
            avgProgress = Math.Round(validForProgress.Average(c => c.ProgressPercentage), 1);
        }

        var now = DateTime.UtcNow;
        int currentSemester = courses.Count(c =>
            c.AcademicStatus == "Active" ||
            (c.StartDate.HasValue && c.EndDate.HasValue && c.StartDate.Value <= now && c.EndDate.Value >= now));

        int upcomingClasses = 0;
        foreach (var course in courses.Where(c => c.AcademicStatus == "Active" || c.AcademicStatus == "Upcoming"))
        {
            for (int i = 0; i < 7; i++)
            {
                var checkDate = now.AddDays(i);
                if (course.StartDate.HasValue && checkDate.Date < course.StartDate.Value.Date) continue;
                if (course.EndDate.HasValue && checkDate.Date > course.EndDate.Value.Date) continue;

                if (course.Schedules.Any(s => s.DayOfWeek == checkDate.DayOfWeek))
                {
                    upcomingClasses++;
                }
            }
        }

        return new StudentCoursesStatisticsDto
        {
            TotalEnrolledCourses = total,
            ActiveCourses = active,
            CompletedCourses = completed,
            UpcomingCourses = upcoming,
            TotalCredits = totalCredits,
            OverallProgressPercentage = avgProgress,
            CurrentSemesterCourses = currentSemester > 0 ? currentSemester : active,
            UpcomingClassesCount = upcomingClasses
        };
    }
}
