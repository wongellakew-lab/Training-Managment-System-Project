using System.Globalization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.DashboardDtos;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Grading;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class DashboardService(
    TmsDbContext _context,
    IMapper _mapper) : IDashboardService
{

    private static readonly string[] MonthNames =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private static readonly string[] Colors =
        { "#1E40AF", "#0284C7", "#38BDF8", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#06B6D4" };

    // ─── Student Dashboard ─────────────────────────────────────────────────
    public async Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c!.Instructor)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Modules)
                    .ThenInclude(m => m.Lessons)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Schedules)
            .Where(e => e.StudentId == studentId && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected && !e.IsDeleted)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var activeEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Active).ToList();
        var completedEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Completed).ToList();
        var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();

        // Calculate dynamic GPA from graded enrollments and course credits on a standard 4.00 scale
        var gradedEnrollments = enrollments.Where(e => e.Grade.HasValue && e.Course != null).ToList();
        decimal cumulativeGpa = 0.0m;
        var gradingService = new GradingService();
        if (gradedEnrollments.Any())
        {
            var courseTuples = gradedEnrollments.Select(e => (e.Grade!.Value, e.Course?.Credits > 0 ? e.Course.Credits : 3));
            cumulativeGpa = gradingService.CalculateEnrollmentGPA(courseTuples);
        }
        else if (student != null && student.Gpa > 0)
        {
            cumulativeGpa = gradingService.CalculateGradePoint(student.Gpa);
        }

        // Earned credits from completed courses
        var earnedCredits = completedEnrollments
            .Where(e => e.Course != null)
            .Sum(e => e.Course!.Credits);

        var certCount = await _context.Certificates
            .AsNoTracking()
            .CountAsync(c => c.StudentId == studentId, cancellationToken);

        // Upcoming classes from schedules for enrolled active/upcoming courses strictly
        var nowTime = DateTime.UtcNow;
        var upcomingCandidates = new List<(DateTime StartDateTime, StudentUpcomingClassDto Dto)>();

        if (enrolledCourseIds.Any())
        {
            // A) Explicit schedules in PostgreSQL
            var explicitSchedules = await _context.Schedules
                .AsNoTracking()
                .Include(s => s.Course)
                    .ThenInclude(c => c!.Instructor)
                .Where(s => !s.IsDeleted && s.CourseId.HasValue && enrolledCourseIds.Contains(s.CourseId.Value) && s.Date.Date >= nowTime.Date.AddDays(-1))
                .ToListAsync(cancellationToken);

            foreach (var s in explicitSchedules)
            {
                TryParseTime(s.StartTime, out var sStart);
                var startDt = s.Date.Date.Add(sStart);
                if (startDt > nowTime)
                {
                    upcomingCandidates.Add((startDt, new StudentUpcomingClassDto
                    {
                        ScheduleId = s.Id,
                        CourseId = s.CourseId,
                        CourseTitle = s.Course?.Title ?? s.Title,
                        CourseCode = s.Course?.Code ?? "TMS",
                        Title = s.Title,
                        Category = s.Category,
                        ScheduleType = s.ScheduleType,
                        Date = s.Date,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Location = s.Location,
                        InstructorName = s.Course?.Instructor != null
                            ? $"{s.Course.Instructor.FirstName} {s.Course.Instructor.LastName}"
                            : (!string.IsNullOrEmpty(s.PicName) ? s.PicName : "Instructor"),
                        Notes = s.Notes
                    }));
                }
            }

            // B) Recurring weekly CourseSchedules projected into the future
            foreach (var en in enrollments.Where(e => e.Course != null && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected))
            {
                var course = en.Course!;
                if (course.Schedules == null || !course.Schedules.Any()) continue;

                var maxProjectionDays = 60;
                var maxDate = course.EndDate.Date < nowTime.Date.AddDays(maxProjectionDays) ? course.EndDate.Date : nowTime.Date.AddDays(maxProjectionDays);

                for (var checkDate = nowTime.Date; checkDate <= maxDate; checkDate = checkDate.AddDays(1))
                {
                    if (checkDate < course.StartDate.Date || checkDate > course.EndDate.Date) continue;

                    foreach (var cs in course.Schedules.Where(s => !s.IsDeleted && s.DayOfWeek == checkDate.DayOfWeek))
                    {
                        TryParseTime(cs.StartTime, out var csStart);
                        var startDt = checkDate.Add(csStart);
                        if (startDt > nowTime)
                        {
                            if (upcomingCandidates.Any(c => c.Dto.CourseId == course.Id && c.Dto.Date.Date == checkDate && c.Dto.StartTime == cs.StartTime))
                                continue;

                            upcomingCandidates.Add((startDt, new StudentUpcomingClassDto
                            {
                                ScheduleId = Math.Abs(HashCode.Combine(cs.Id, checkDate.Year, checkDate.Month, checkDate.Day)),
                                CourseId = course.Id,
                                CourseTitle = course.Title,
                                CourseCode = course.Code,
                                Title = $"{course.Title} - {cs.ScheduleType}",
                                Category = course.Category,
                                ScheduleType = cs.ScheduleType,
                                Date = checkDate,
                                StartTime = cs.StartTime,
                                EndTime = cs.EndTime,
                                Location = !string.IsNullOrWhiteSpace(cs.Room) ? $"{cs.Location} ({cs.Room})" : cs.Location,
                                InstructorName = course.Instructor != null
                                    ? $"{course.Instructor.FirstName} {course.Instructor.LastName}"
                                    : "Faculty Instructor",
                                Notes = $"Weekly {cs.ScheduleType} session"
                            }));
                        }
                    }
                }
            }
        }

        // Strictly order chronologically and take a MAXIMUM OF 3
        var upcomingClassDtos = upcomingCandidates
            .OrderBy(c => c.StartDateTime)
            .Take(3)
            .Select(c => c.Dto)
            .ToList();

        // Enrolled courses' assignments and student submissions
        var enrolledAssignments = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Course)
            .Where(a => enrolledCourseIds.Contains(a.CourseId) && !a.IsDeleted && a.IsPublished)
            .OrderBy(a => a.DueDate)
            .ToListAsync(cancellationToken);

        var allStudentAssessments = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && enrolledCourseIds.Contains(a.CourseId))
            .ToListAsync(cancellationToken);

        // Pending assignments: strictly those not yet submitted or marked pending
        var pendingDtos = new List<StudentPendingAssignmentDto>();
        foreach (var assign in enrolledAssignments)
        {
            var sub = allStudentAssessments.FirstOrDefault(s => s.AssignmentId == assign.Id);
            if (sub == null || sub.Status == "Pending")
            {
                pendingDtos.Add(new StudentPendingAssignmentDto
                {
                    Id = sub?.Id ?? assign.Id,
                    CourseId = assign.CourseId,
                    CourseTitle = assign.Course?.Title ?? "Course Assessment",
                    CourseCode = assign.Course?.Code ?? "",
                    Title = assign.Title,
                    MaxScore = assign.MaxPoints,
                    DueDate = assign.DueDate,
                    Status = sub?.Status ?? "Pending"
                });
            }
        }
        pendingDtos = pendingDtos.Take(6).ToList();

        // Recent published grades with feedback (filtered strictly to enrolled courses)
        var recentGradedAssessments = allStudentAssessments
            .Where(a => a.IsPublished && a.ScoredPoints.HasValue)
            .OrderByDescending(a => a.GradedAt ?? a.DueDate)
            .Take(5)
            .ToList();

        var recentGradeDtos = recentGradedAssessments.Select(a =>
        {
            var course = enrollments.FirstOrDefault(e => e.CourseId == a.CourseId)?.Course;
            var pct = a.MaxScore > 0 ? (a.ScoredPoints!.Value / a.MaxScore) * 100m : 0m;
            string letter = pct >= 90 ? "A" : pct >= 80 ? "B" : pct >= 70 ? "C" : pct >= 60 ? "D" : "F";
            return new StudentRecentGradeDto
            {
                AssessmentId = a.Id,
                CourseId = a.CourseId,
                CourseTitle = course?.Title ?? "Course",
                CourseCode = course?.Code ?? "",
                AssessmentTitle = a.Title,
                ScoredPoints = a.ScoredPoints!.Value,
                MaxScore = a.MaxScore,
                LetterGrade = letter,
                Feedback = a.Feedback,
                GradedAt = a.GradedAt
            };
        }).ToList();

        // Course progress based on authoritative assessment configuration and evaluated assessments
        var courseProgressList = new List<StudentCourseProgressDto>();
        foreach (var en in enrollments.Where(e => e.Course != null))
        {
            var course = en.Course!;
            var courseAssessments = allStudentAssessments.Where(a => a.CourseId == en.CourseId).ToList();

            int totalExpectedAssessments = course.ExpectedAssignmentCount + course.ExpectedMidtermCount + course.ExpectedFinalCount;
            if (totalExpectedAssessments == 0)
            {
                var courseAssignments = enrolledAssignments.Where(a => a.CourseId == en.CourseId).ToList();
                totalExpectedAssessments = courseAssignments.Count > 0 ? courseAssignments.Count : 1;
            }

            // ONLY evaluated assessments (Graded and ScoredPoints.HasValue) count toward progress
            int evaluatedAssessments = courseAssessments.Count(a => a.Status == "Graded" && a.ScoredPoints.HasValue);

            // 100% is strictly allowed ONLY when completed assessments == total assessments
            decimal completionPct = totalExpectedAssessments > 0
                ? Math.Min(100m, Math.Round((evaluatedAssessments / (decimal)totalExpectedAssessments) * 100m, 1))
                : 0m;

            if (evaluatedAssessments < totalExpectedAssessments && completionPct >= 100m)
            {
                completionPct = Math.Min(99m, completionPct);
            }

            int totalModules = course.Modules.Count;
            int completedModules = totalModules > 0 ? Math.Min(totalModules, (int)Math.Round((completionPct / 100m) * totalModules)) : 0;

            courseProgressList.Add(new StudentCourseProgressDto
            {
                CourseId = en.CourseId,
                CourseTitle = course.Title,
                CourseCode = course.Code,
                CompletedAssignments = evaluatedAssessments,
                TotalAssignments = totalExpectedAssessments,
                CompletedAssessments = evaluatedAssessments,
                TotalAssessments = totalExpectedAssessments,
                CompletedModules = completedModules,
                TotalModules = totalModules,
                CompletionPercentage = completionPct,
                Status = en.Status.ToString(),
                CurrentGrade = en.Grade
            });
        }

        // Unread notifications count
        var studentUserId = student?.UserId ?? studentId.ToString();
        var unreadNotifications = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => (n.UserId == studentUserId || n.UserId == studentId.ToString()) && !n.IsRead, cancellationToken);

        return new StudentDashboardDto
        {
            CumulativeGpa = cumulativeGpa,
            ActiveEnrollmentsCount = activeEnrollments.Count,
            CompletedCoursesCount = completedEnrollments.Count,
            EarnedCredits = earnedCredits,
            CertificatesCount = certCount,
            TotalEnrollmentsCount = enrollments.Count,
            CurrentSemester = "Summer 2030",
            UnreadNotificationCount = unreadNotifications,
            UpcomingClasses = upcomingClassDtos,
            PendingAssignments = pendingDtos,
            RecentGrades = recentGradeDtos,
            CourseProgress = courseProgressList,
            RecentEnrollments = _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments.Take(10)),
            UpcomingSchedules = upcomingClassDtos.Select(c => new ScheduleDto
            {
                Id = c.ScheduleId,
                CourseId = c.CourseId,
                CourseTitle = c.CourseTitle,
                CourseCode = c.CourseCode,
                Title = c.Title,
                Category = c.Category,
                ScheduleType = c.ScheduleType,
                Date = c.Date,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                Location = c.Location,
                Notes = c.Notes
            }).ToList()
        };
    }

    // ─── Instructor Dashboard ───────────────────────────────────────────────
    private static readonly string[] TimeFormats = {
        "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt",
        "H:mm", "HH:mm", "h:mm:ss tt", "hh:mm:ss tt", "H:mm:ss", "HH:mm:ss"
    };

    private static bool TryParseTime(string? timeStr, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(timeStr)) return false;
        if (DateTime.TryParseExact(timeStr.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            timeSpan = dt.TimeOfDay;
            return true;
        }
        if (TimeSpan.TryParse(timeStr.Trim(), CultureInfo.InvariantCulture, out timeSpan))
        {
            return true;
        }
        return false;
    }

    public async Task<InstructorDashboardDto> GetInstructorDashboardAsync(int instructorId, DateTime? clientTime = null, CancellationToken cancellationToken = default)
    {
        var assignedCourses = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Schedules)
            .Where(c => c.InstructorId == instructorId && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var assignedCourseIds = assignedCourses.Select(c => c.Id).ToList();

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => assignedCourseIds.Contains(e.CourseId) && !e.IsDeleted)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        var activeStudentIds = enrollments
            .Where(e => e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed)
            .Select(e => e.StudentId)
            .Distinct()
            .Count();

        var pendingEnrollments = enrollments.Count(e => e.Status == EnrollmentStatus.Pending);

        var certCount = await _context.Certificates
            .AsNoTracking()
            .CountAsync(c => assignedCourseIds.Contains(c.CourseId), cancellationToken);

        var now = clientTime ?? DateTime.Now;
        var today = now.Date;
        var todayDayOfWeek = today.DayOfWeek;
        var currentTimeOfDay = now.TimeOfDay;

        // 1. Today's Classes (from Schedules or recurring CourseSchedules)
        // Must only show classes actually happening today (in progress or completed today)
        // Classes starting in the future today belong to Upcoming Sessions
        var todaySchedules = await _context.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Where(s => !s.IsDeleted && s.CourseId.HasValue && assignedCourseIds.Contains(s.CourseId.Value) && s.Date.Date == today)
            .ToListAsync(cancellationToken);

        var todayClassDtos = new List<InstructorTodayClassDto>();

        foreach (var ts in todaySchedules)
        {
            var cid = ts.CourseId!.Value;
            var studentsCount = enrollments.Count(e => e.CourseId == cid && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));
            var hasAttendance = await _context.Attendances
                .AsNoTracking()
                .AnyAsync(a => a.CourseId == cid && a.SessionDate.Date == today, cancellationToken);

            TryParseTime(ts.StartTime, out var startSpan);
            TryParseTime(ts.EndTime, out var endSpan);
            if (endSpan <= startSpan) endSpan = startSpan.Add(TimeSpan.FromHours(1.5));

            if (currentTimeOfDay < startSpan)
            {
                // Future class today -> NOT shown as currently active today's class
                continue;
            }

            var status = currentTimeOfDay >= startSpan && currentTimeOfDay <= endSpan ? "In Progress" : "Completed";

            todayClassDtos.Add(new InstructorTodayClassDto
            {
                ScheduleId = ts.Id,
                CourseId = cid,
                CourseName = ts.Course?.Title ?? ts.Title,
                CourseCode = ts.Course?.Code ?? "CRS",
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                Room = !string.IsNullOrWhiteSpace(ts.Location) ? ts.Location : "Room 204",
                Location = !string.IsNullOrWhiteSpace(ts.Location) ? ts.Location : "Main Training Center",
                StudentCount = studentsCount,
                AttendanceStatus = hasAttendance ? "Taken" : "Pending",
                Status = status
            });
        }

        // Process recurring CourseSchedules for today
        foreach (var course in assignedCourses)
        {
            if (course.Schedules == null || !course.Schedules.Any()) continue;
            if (today < course.StartDate.Date || today > course.EndDate.Date) continue;

            var recurringToday = course.Schedules.Where(cs => !cs.IsDeleted && cs.DayOfWeek == todayDayOfWeek).ToList();
            foreach (var recurring in recurringToday)
            {
                if (todaySchedules.Any(ts => ts.CourseId == course.Id && ts.StartTime == recurring.StartTime))
                    continue;

                TryParseTime(recurring.StartTime, out var startSpan);
                TryParseTime(recurring.EndTime, out var endSpan);
                if (endSpan <= startSpan) endSpan = startSpan.Add(TimeSpan.FromHours(1.5));

                if (currentTimeOfDay < startSpan)
                {
                    // Future class today -> NOT shown as currently active today's class
                    continue;
                }

                var status = currentTimeOfDay >= startSpan && currentTimeOfDay <= endSpan ? "In Progress" : "Completed";
                var studentsCount = enrollments.Count(e => e.CourseId == course.Id && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));
                var hasAttendance = await _context.Attendances
                    .AsNoTracking()
                    .AnyAsync(a => a.CourseId == course.Id && a.SessionDate.Date == today, cancellationToken);

                todayClassDtos.Add(new InstructorTodayClassDto
                {
                    ScheduleId = recurring.Id,
                    CourseId = course.Id,
                    CourseName = course.Title,
                    CourseCode = course.Code,
                    StartTime = recurring.StartTime,
                    EndTime = recurring.EndTime,
                    Room = !string.IsNullOrWhiteSpace(recurring.Room) ? recurring.Room : "Room 204",
                    Location = !string.IsNullOrWhiteSpace(recurring.Location) ? recurring.Location : "Main Training Center",
                    StudentCount = studentsCount,
                    AttendanceStatus = hasAttendance ? "Taken" : "Pending",
                    Status = status
                });
            }
        }

        todayClassDtos = todayClassDtos.OrderBy(c => c.StartTime).ToList();

        // 2. Pending Grading (Assignments with submissions in "Submitted" status awaiting review)
        var pendingSubmissions = await _context.Assessments
            .AsNoTracking()
            .Include(a => a.Course)
            .Where(a => assignedCourseIds.Contains(a.CourseId) && a.Status == "Submitted")
            .ToListAsync(cancellationToken);

        var pendingGradingDtos = pendingSubmissions
            .GroupBy(a => new { a.AssignmentId, a.CourseId, a.Title, CourseTitle = a.Course != null ? a.Course.Title : "Course", a.DueDate })
            .Select(g => new InstructorPendingGradingDto
            {
                AssignmentId = g.Key.AssignmentId ?? 0,
                CourseId = g.Key.CourseId,
                AssignmentTitle = g.Key.Title,
                CourseName = g.Key.CourseTitle,
                SubmissionsCount = g.Count(),
                DueDate = g.Key.DueDate
            })
            .OrderBy(pg => pg.DueDate)
            .Take(6)
            .ToList();

        // 3. Upcoming Sessions (Real upcoming sessions from PostgreSQL for this instructor)
        var upcomingCandidates = new List<(DateTime StartDateTime, InstructorUpcomingSessionDto Dto)>();

        // A) Explicit schedules in the future
        var explicitUpcoming = await _context.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Where(s => !s.IsDeleted && s.CourseId.HasValue && assignedCourseIds.Contains(s.CourseId.Value) && s.Date.Date >= today)
            .ToListAsync(cancellationToken);

        foreach (var s in explicitUpcoming)
        {
            TryParseTime(s.StartTime, out var sStart);
            var startDt = s.Date.Date.Add(sStart);
            if (startDt > now)
            {
                var studentsCount = enrollments.Count(e => e.CourseId == s.CourseId!.Value && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));
                upcomingCandidates.Add((startDt, new InstructorUpcomingSessionDto
                {
                    ScheduleId = s.Id,
                    CourseId = s.CourseId!.Value,
                    CourseName = s.Course?.Title ?? s.Title,
                    Date = s.Date.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Location = !string.IsNullOrWhiteSpace(s.Location) ? s.Location : "Main Training Center",
                    Room = !string.IsNullOrWhiteSpace(s.Location) ? s.Location : "Room 204",
                    EnrolledStudentsCount = studentsCount
                }));
            }
        }

        // B) Recurring course schedules projected into the future
        foreach (var course in assignedCourses)
        {
            if (course.Schedules == null || !course.Schedules.Any()) continue;
            var enrolledCount = enrollments.Count(e => e.CourseId == course.Id && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));

            var maxProjectionDays = 60;
            var maxDate = course.EndDate.Date < today.AddDays(maxProjectionDays) ? course.EndDate.Date : today.AddDays(maxProjectionDays);

            for (var checkDate = today; checkDate <= maxDate; checkDate = checkDate.AddDays(1))
            {
                if (checkDate < course.StartDate.Date || checkDate > course.EndDate.Date) continue;

                foreach (var cs in course.Schedules.Where(s => !s.IsDeleted && s.DayOfWeek == checkDate.DayOfWeek))
                {
                    TryParseTime(cs.StartTime, out var csStart);
                    var startDt = checkDate.Add(csStart);
                    if (startDt > now)
                    {
                        if (upcomingCandidates.Any(c => c.Dto.CourseId == course.Id && c.Dto.Date.Date == checkDate && c.Dto.StartTime == cs.StartTime))
                            continue;

                        upcomingCandidates.Add((startDt, new InstructorUpcomingSessionDto
                        {
                            ScheduleId = Math.Abs(HashCode.Combine(cs.Id, checkDate.Year, checkDate.Month, checkDate.Day)),
                            CourseId = course.Id,
                            CourseName = course.Title,
                            Date = checkDate,
                            StartTime = cs.StartTime,
                            EndTime = cs.EndTime,
                            Location = !string.IsNullOrWhiteSpace(cs.Location) ? cs.Location : "Main Training Center",
                            Room = !string.IsNullOrWhiteSpace(cs.Room) ? cs.Room : "Room 204",
                            EnrolledStudentsCount = enrolledCount
                        }));
                    }
                }
            }
        }

        var upcomingSessionDtos = upcomingCandidates
            .OrderBy(c => c.StartDateTime)
            .Take(3)
            .Select(c => c.Dto)
            .ToList();

        // 4. Recent Student Questions
        var studentQuestions = await _context.StudentQuestions
            .AsNoTracking()
            .Include(q => q.Student)
            .Include(q => q.Course)
            .Where(q => assignedCourseIds.Contains(q.CourseId))
            .OrderByDescending(q => q.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentQuestionDtos = studentQuestions.Select(q => new InstructorStudentQuestionDto
        {
            Id = q.Id,
            StudentId = q.StudentId,
            StudentName = q.Student != null ? $"{q.Student.FirstName} {q.Student.LastName}" : "Enrolled Student",
            StudentAvatar = q.Student?.ProfilePictureUrl,
            CourseId = q.CourseId,
            CourseName = q.Course?.Title ?? "Course",
            Question = q.Question,
            Answer = q.Answer,
            Date = q.CreatedAt,
            IsAnswered = q.IsAnswered
        }).ToList();

        // 5. Attendance Rate
        var totalAttendances = await _context.Attendances
            .AsNoTracking()
            .CountAsync(a => assignedCourseIds.Contains(a.CourseId), cancellationToken);

        var presentCount = await _context.Attendances
            .AsNoTracking()
            .CountAsync(a => assignedCourseIds.Contains(a.CourseId) && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late), cancellationToken);

        decimal attendanceRate = totalAttendances > 0
            ? Math.Round(((decimal)presentCount / totalAttendances) * 100m, 1)
            : 0.0m;

        return new InstructorDashboardDto
        {
            AssignedCoursesCount = assignedCourses.Count,
            ActiveStudentsCount = activeStudentIds,
            PendingEnrollmentsCount = pendingEnrollments,
            PendingGradesCount = pendingSubmissions.Count,
            CertificatesAwardedCount = certCount,
            AttendanceRate = attendanceRate,
            TodaysClasses = todayClassDtos,
            PendingGrading = pendingGradingDtos,
            UpcomingSessions = upcomingSessionDtos,
            RecentStudentQuestions = recentQuestionDtos,
            RecentEnrollments = _mapper.Map<IEnumerable<EnrollmentDto>>(enrollments.Take(10)),
            AssignedCourses = _mapper.Map<IEnumerable<CourseDto>>(assignedCourses)
        };
    }

    // ─── Admin Dashboard ────────────────────────────────────────────────────
    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        // Core counts — from real DB
        var totalStudents = await _context.Students.CountAsync(s => !s.IsDeleted, cancellationToken);
        var totalInstructors = await _context.Instructors.CountAsync(i => !i.IsDeleted, cancellationToken);
        var totalCourses = await _context.Courses.CountAsync(c => !c.IsDeleted, cancellationToken);
        var activeEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Active, cancellationToken);
        var pendingEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Pending, cancellationToken);
        var waitlistedEnrollments = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Waitlisted, cancellationToken);
        var totalCertificates = await _context.Certificates.CountAsync(cancellationToken);

        // Pending approval list
        var pendingList = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => e.Status == EnrollmentStatus.Pending)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        // New/recent courses
        var newCoursesEntities = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.Id)
            .Take(4)
            .ToListAsync(cancellationToken);

        // Upcoming schedules (nearest 3 upcoming real schedules)
        var today = DateTime.UtcNow.Date;
        var schedulesList = await _context.Schedules
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Date.Date >= today)
            .OrderBy(s => s.Date)
            .Take(3)
            .ToListAsync(cancellationToken);

        // ─ Enrollment Trends from real data ─────────────────────
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var recentEnrollments = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.EnrolledAt >= sixMonthsAgo)
            .Select(e => new { e.EnrolledAt })
            .ToListAsync(cancellationToken);

        var enrollmentTrendByMonth = recentEnrollments
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyEnrollmentTrendDto
            {
                Month = MonthNames[g.Key.Month - 1],
                Count = g.Count(),
                IsPeak = false
            })
            .ToList();

        // Mark peak month
        if (enrollmentTrendByMonth.Any())
        {
            var maxCount = enrollmentTrendByMonth.Max(t => t.Count);
            foreach (var trend in enrollmentTrendByMonth.Where(t => t.Count == maxCount))
                trend.IsPeak = true;
        }

        // ─ Top Courses from real enrollment counts ─────────────────────────
        var topCourseStats = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.Course != null && !e.Course.IsDeleted)
            .GroupBy(e => new { e.CourseId, e.Course!.Title })
            .Select(g => new { Title = g.Key.Title, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var totalEnrolledForTopN = topCourseStats.Sum(x => x.Count);
        var topCourseDtos = topCourseStats
            .Select((x, i) => new TopCourseStatDto
            {
                Name = x.Title,
                StudentsCount = x.Count,
                Percentage = totalEnrolledForTopN > 0 ? (int)Math.Round((x.Count / (double)totalEnrolledForTopN) * 100) : 0,
                Color = Colors[i % Colors.Length]
            })
            .ToList();

        // ─ Recent System Activities with real resolved actors ──────────────
        var activityList = new List<SystemActivityDto>();

        // 1. Recent enrollments (resolving student actor & course)
        var recentEnrollmentsForActivities = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(15)
            .ToListAsync(cancellationToken);

        foreach (var en in recentEnrollmentsForActivities)
        {
            var studentName = en.Student != null
                ? $"{en.Student.FirstName} {en.Student.LastName}".Trim()
                : "Student";
            var courseTitle = en.Course?.Title ?? "Course";
            var courseCode = !string.IsNullOrWhiteSpace(en.Course?.Code) ? $" ({en.Course.Code})" : "";
            var isPending = en.Status == EnrollmentStatus.Pending;

            activityList.Add(new SystemActivityDto
            {
                Id = $"enrollment-{en.Id}",
                Title = isPending ? "Enrollment Submitted" : $"Enrollment {en.Status}",
                Description = isPending
                    ? $"Student {studentName} submitted an enrollment request for '{courseTitle}'{courseCode}."
                    : $"Student {studentName} enrolled in '{courseTitle}'{courseCode}.",
                Type = "Enrollment",
                ActorName = studentName,
                ActorRole = "Student",
                UserAvatar = en.Student?.User?.ProfilePictureUrl ?? "",
                Timestamp = en.EnrolledAt
            });
        }

        // 2. Recent courses (resolving instructor actor)
        var recentCoursesForActivities = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Instructor)!.ThenInclude(i => i!.User)
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var c in recentCoursesForActivities)
        {
            var instName = c.Instructor != null
                ? $"{c.Instructor.FirstName} {c.Instructor.LastName}".Trim()
                : "Faculty Instructor";
            var isUnderReview = string.Equals(c.Status, "Under Review", StringComparison.OrdinalIgnoreCase);
            var isPublished = string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(c.Status, "Published", StringComparison.OrdinalIgnoreCase);
            var isRejected = string.Equals(c.Status, "Rejected", StringComparison.OrdinalIgnoreCase);

            string title;
            string description;
            if (isUnderReview)
            {
                title = "Course Under Review";
                description = $"Instructor {instName} submitted '{c.Title}' ({c.Code}) for review.";
            }
            else if (isRejected)
            {
                title = "Course Rejected";
                description = $"Admin rejected course '{c.Title}' ({c.Code}).";
            }
            else if (isPublished)
            {
                title = "Course Approved";
                description = $"Admin approved '{c.Title}' ({c.Code}).";
            }
            else
            {
                title = $"Course {c.Status}";
                description = $"Instructor {instName} created draft '{c.Title}' ({c.Code}).";
            }

            activityList.Add(new SystemActivityDto
            {
                Id = $"course-{c.Id}",
                Title = title,
                Description = description,
                Type = "Course",
                ActorName = isRejected || isPublished ? "Admin" : instName,
                ActorRole = isRejected || isPublished ? "Admin" : "Instructor",
                UserAvatar = c.Instructor?.User?.ProfilePictureUrl ?? "",
                Timestamp = c.StartDate
            });
        }

        // Combined recent activities sorted newest first
        var recentActivities = activityList
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .ToList();

        return new AdminDashboardDto
        {
            TotalStudents = totalStudents,
            TotalInstructors = totalInstructors,
            TotalCourses = totalCourses,
            ActiveEnrollments = activeEnrollments,
            PendingEnrollments = pendingEnrollments,
            WaitlistedEnrollments = waitlistedEnrollments,
            TotalCertificates = totalCertificates,

            EnrollmentGrowthPercentage = enrollmentTrendByMonth.Count >= 2
                ? CalculateGrowth(enrollmentTrendByMonth[^2].Count, enrollmentTrendByMonth[^1].Count)
                : 0m,
            EnrollmentTrends = enrollmentTrendByMonth,

            TopCoursesDistribution = topCourseDtos,

            NewCourses = _mapper.Map<List<CourseDto>>(newCoursesEntities),
            UpcomingSchedules = _mapper.Map<List<ScheduleDto>>(schedulesList),
            RecentActivities = recentActivities,
            PendingApprovalList = _mapper.Map<IEnumerable<EnrollmentDto>>(pendingList),
            RecentActivity = recentActivities
        };
    }

    private static decimal CalculateGrowth(int previous, int current)
    {
        if (previous == 0) return current > 0 ? 100m : 0m;
        return Math.Round(((current - previous) / (decimal)previous) * 100, 1);
    }
}

