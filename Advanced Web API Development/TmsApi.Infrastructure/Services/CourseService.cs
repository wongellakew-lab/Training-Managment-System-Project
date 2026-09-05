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

        if (course == null) return null;
        var dto = _mapper.Map<CourseDto>(course);
        dto.CreatedAssignmentsCount = await _context.Assignments.CountAsync(a => a.CourseId == id && !a.IsDeleted, cancellationToken);
        return dto;
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var course = _mapper.Map<Course>(dto);
        course.ExpectedMidtermCount = dto.HasMidterm ? 1 : 0;
        course.ExpectedFinalCount = dto.HasFinal ? 1 : 0;
        if (dto.Schedules != null && dto.Schedules.Count > 0)
        {
            course.Schedules = dto.Schedules.Select(s => new CourseSchedule
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Location = !string.IsNullOrWhiteSpace(s.Location) ? s.Location : "Main Training Center",
                Room = s.Room ?? string.Empty,
                ScheduleType = !string.IsNullOrWhiteSpace(s.ScheduleType) ? s.ScheduleType : "Lecture"
            }).ToList();
        }

        if (dto.Modules != null && dto.Modules.Count > 0)
        {
            course.Modules = dto.Modules.Select((m, mIdx) => new CourseModule
            {
                Title = m.Title,
                Description = m.Description,
                OrderIndex = m.OrderIndex > 0 ? m.OrderIndex : mIdx + 1,
                Lessons = (m.Lessons ?? new List<CourseLessonDto>()).Select((l, lIdx) => new CourseLesson
                {
                    Title = l.Title,
                    Description = l.Description,
                    DurationMinutes = l.DurationMinutes > 0 ? l.DurationMinutes : 45,
                    ContentText = l.ContentText,
                    PdfUrl = l.PdfUrl,
                    PdfFileName = l.PdfFileName,
                    OrderIndex = l.OrderIndex > 0 ? l.OrderIndex : lIdx + 1
                }).ToList()
            }).ToList();
        }

        await _context.Courses.AddAsync(course, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CourseDto>(course);
    }

    public async Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (course == null || course.IsDeleted) return false;

        var originalInstructorId = course.InstructorId;
        var originalCode = course.Code;

        _mapper.Map(dto, course);
        course.ExpectedMidtermCount = dto.HasMidterm ? 1 : 0;
        course.ExpectedFinalCount = dto.HasFinal ? 1 : 0;

        if (string.IsNullOrWhiteSpace(course.Code))
        {
            course.Code = originalCode;
        }

        // Ensure instructor ownership is preserved
        if (dto.InstructorId.HasValue && dto.InstructorId.Value > 0)
        {
            course.InstructorId = dto.InstructorId.Value;
        }
        else if (originalInstructorId.HasValue)
        {
            course.InstructorId = originalInstructorId.Value;
        }

        // Sync schedules if provided in dto
        if (dto.Schedules != null && dto.Schedules.Count > 0)
        {
            _context.CourseSchedules.RemoveRange(course.Schedules);
            course.Schedules = dto.Schedules.Select(s => new CourseSchedule
            {
                CourseId = course.Id,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Location = !string.IsNullOrWhiteSpace(s.Location) ? s.Location : "Main Training Center",
                Room = s.Room ?? string.Empty,
                ScheduleType = !string.IsNullOrWhiteSpace(s.ScheduleType) ? s.ScheduleType : "Lecture"
            }).ToList();
        }

        // Sync modules and lessons if provided in dto
        if (dto.Modules != null && dto.Modules.Count > 0)
        {
            var dtoModIds = dto.Modules.Where(m => m.Id > 0).Select(m => m.Id).ToHashSet();
            var modulesToRemove = course.Modules.Where(m => m.Id > 0 && !dtoModIds.Contains(m.Id)).ToList();
            foreach (var modToRemove in modulesToRemove)
            {
                course.Modules.Remove(modToRemove);
                _context.CourseModules.Remove(modToRemove);
            }

            foreach (var modDto in dto.Modules)
            {
                var existingMod = course.Modules.FirstOrDefault(m => m.Id != 0 && m.Id == modDto.Id);
                if (existingMod != null)
                {
                    existingMod.Title = modDto.Title;
                    existingMod.Description = modDto.Description;
                    existingMod.OrderIndex = modDto.OrderIndex;

                    if (modDto.Lessons != null)
                    {
                        var dtoLessonIds = modDto.Lessons.Where(l => l.Id > 0).Select(l => l.Id).ToHashSet();
                        var lessonsToRemove = existingMod.Lessons.Where(l => l.Id > 0 && !dtoLessonIds.Contains(l.Id)).ToList();
                        foreach (var lessonToRemove in lessonsToRemove)
                        {
                            existingMod.Lessons.Remove(lessonToRemove);
                            _context.CourseLessons.Remove(lessonToRemove);
                        }

                        foreach (var lessDto in modDto.Lessons)
                        {
                            var existingLesson = existingMod.Lessons.FirstOrDefault(l => l.Id != 0 && l.Id == lessDto.Id);
                            if (existingLesson != null)
                            {
                                existingLesson.Title = lessDto.Title;
                                existingLesson.Description = lessDto.Description;
                                existingLesson.DurationMinutes = lessDto.DurationMinutes;
                                existingLesson.ContentText = lessDto.ContentText;
                                existingLesson.PdfUrl = lessDto.PdfUrl;
                                existingLesson.PdfFileName = lessDto.PdfFileName;
                                existingLesson.OrderIndex = lessDto.OrderIndex;
                            }
                            else
                            {
                                existingMod.Lessons.Add(new CourseLesson
                                {
                                    Title = lessDto.Title,
                                    Description = lessDto.Description,
                                    DurationMinutes = lessDto.DurationMinutes,
                                    ContentText = lessDto.ContentText,
                                    PdfUrl = lessDto.PdfUrl,
                                    PdfFileName = lessDto.PdfFileName,
                                    OrderIndex = lessDto.OrderIndex
                                });
                            }
                        }
                    }
                }
                else
                {
                    var newMod = new CourseModule
                    {
                        CourseId = course.Id,
                        Title = modDto.Title,
                        Description = modDto.Description,
                        OrderIndex = modDto.OrderIndex,
                        Lessons = (modDto.Lessons ?? new List<CourseLessonDto>()).Select(l => new CourseLesson
                        {
                            Title = l.Title,
                            Description = l.Description,
                            DurationMinutes = l.DurationMinutes,
                            ContentText = l.ContentText,
                            PdfUrl = l.PdfUrl,
                            PdfFileName = l.PdfFileName,
                            OrderIndex = l.OrderIndex
                        }).ToList()
                    };
                    course.Modules.Add(newMod);
                }
            }
        }

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

        var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
        var allCourseAssignments = await _context.Assignments
            .Where(a => courseIds.Contains(a.CourseId) && !a.IsDeleted && a.IsPublished)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var studentAssessments = await _context.Assessments
            .Where(a => a.StudentId == studentId && courseIds.Contains(a.CourseId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var result = new List<StudentEnrolledCourseDto>();

        foreach (var en in enrollments)
        {
            var course = en.Course!;
            var courseAssignments = allCourseAssignments.Where(a => a.CourseId == en.CourseId).ToList();
            var courseAssessments = studentAssessments.Where(a => a.CourseId == en.CourseId).ToList();

            int totalLessons = course.Modules.SelectMany(m => m.Lessons).Count();
            if (totalLessons == 0) totalLessons = course.LessonsCount;

            int totalModules = course.Modules.Count;
            int totalExpectedAssessments = course.ExpectedAssignmentCount + course.ExpectedMidtermCount + course.ExpectedFinalCount;
            if (totalExpectedAssessments <= 0)
            {
                totalExpectedAssessments = courseAssignments.Count > 0 ? courseAssignments.Count : 1;
            }

            // ONLY evaluated assessments (Graded and ScoredPoints.HasValue) count toward progress
            int evaluatedAssessments = courseAssessments.Count(a => a.Status == "Graded" && a.ScoredPoints.HasValue);

            // 100% is strictly allowed ONLY when completed assessments == total assessments
            decimal progressPct = totalExpectedAssessments > 0
                ? Math.Min(100m, Math.Round(((decimal)evaluatedAssessments / totalExpectedAssessments) * 100m, 1))
                : 0m;

            if (evaluatedAssessments < totalExpectedAssessments && progressPct >= 100m)
            {
                progressPct = Math.Min(99m, progressPct);
            }

            int completedLessons = totalLessons > 0 ? Math.Min(totalLessons, (int)Math.Round((progressPct / 100m) * totalLessons)) : 0;
            int completedModules = totalModules > 0 ? Math.Min(totalModules, (int)Math.Round((progressPct / 100m) * totalModules)) : 0;

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
                CompletedAssessments = evaluatedAssessments,
                TotalAssessments = totalExpectedAssessments,
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
