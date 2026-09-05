using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Extensions;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Grading;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/student/progress")]
[Authorize(Roles = "Student,Admin")]
public class StudentProgressController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly GradingService _gradingService;

    public StudentProgressController(TmsDbContext context, GradingService gradingService)
    {
        _context = context;
        _gradingService = gradingService;
    }

    [HttpGet("{courseId:int}")]
    public async Task<IActionResult> GetCourseProgressDetails(int courseId, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (studentId == null)
        {
            return Forbid("Authenticated user is not linked to a student profile.");
        }

        // Verify student is enrolled in the course
        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
            .Include(e => e.Course)
                .ThenInclude(c => c.Schedules)
            .FirstOrDefaultAsync(e => e.StudentId == studentId.Value && e.CourseId == courseId && !e.IsDeleted, cancellationToken);

        if (enrollment == null || enrollment.Course == null || enrollment.Course.IsDeleted)
        {
            return NotFound(new { message = "You are not enrolled in this course or the course does not exist." });
        }

        var course = enrollment.Course;

        // 1. Course Summary
        var courseSummary = new CourseSummaryDto
        {
            CourseId = course.Id,
            CourseCode = course.Code,
            CourseTitle = course.Title,
            Description = course.Description,
            Department = course.Department,
            Level = course.Level,
            Category = course.Category,
            Credits = course.Credits,
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            EnrollmentStatus = enrollment.Status.ToString(),
            InstructorName = course.Instructor != null
                ? $"{course.Instructor.FirstName} {course.Instructor.LastName}".Trim()
                : "Not Assigned",
            InstructorEmail = course.Instructor?.Email ?? string.Empty
        };

        // 2. Attendance
        var attendances = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == studentId.Value && a.CourseId == courseId)
            .OrderByDescending(a => a.SessionDate)
            .ToListAsync(cancellationToken);

        int presentCount = attendances.Count(a => a.Status == AttendanceStatus.Present);
        int lateCount = attendances.Count(a => a.Status == AttendanceStatus.Late);
        int absentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        int totalSessions = attendances.Count;
        decimal attendanceRate = totalSessions > 0
            ? Math.Round(((decimal)(presentCount + lateCount) / totalSessions) * 100m, 1)
            : 0m;

        var attendanceSummary = new AttendanceSummaryDto
        {
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            TotalSessions = totalSessions,
            AttendanceRate = attendanceRate,
            Sessions = attendances.Select(a => new AttendanceSessionDto
            {
                Id = a.Id,
                SessionDate = a.SessionDate,
                Status = a.Status.ToString(),
                Remarks = a.Remarks
            }).ToList()
        };

        // 3. Assignments & Assessments
        var assignments = await _context.Assignments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && !a.IsDeleted)
            .OrderBy(a => a.AssignmentNumber)
            .ThenBy(a => a.DueDate)
            .ToListAsync(cancellationToken);

        var studentAssessments = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.StudentId == studentId.Value && a.CourseId == courseId)
            .ToListAsync(cancellationToken);

        var assignmentSections = new AssignmentSectionsDto();

        foreach (var a in assignments)
        {
            var studentAssessment = studentAssessments.FirstOrDefault(sub => sub.AssignmentId == a.Id);

            if (studentAssessment != null && studentAssessment.Status == "Graded" && studentAssessment.ScoredPoints.HasValue)
            {
                assignmentSections.Completed.Add(new CompletedAssignmentDto
                {
                    AssignmentId = a.Id,
                    AssignmentNumber = a.AssignmentNumber,
                    Title = a.Title,
                    DueDate = a.DueDate,
                    MaxPoints = a.MaxPoints,
                    ScoredPoints = studentAssessment.ScoredPoints.Value,
                    GradedAt = studentAssessment.GradedAt,
                    Feedback = studentAssessment.Feedback
                });
            }
            else
            {
                assignmentSections.InProgress.Add(new InProgressAssignmentDto
                {
                    AssignmentId = a.Id,
                    AssignmentNumber = a.AssignmentNumber,
                    Title = a.Title,
                    DueDate = a.DueDate,
                    MaxPoints = a.MaxPoints,
                    SubmissionStatus = studentAssessment != null && studentAssessment.Status == "Submitted" ? "Submitted" : "Not Submitted",
                    SubmittedAt = studentAssessment?.SubmittedAt
                });
            }
        }

        // 4. Authoritative Course Progress
        int completedAssessments = studentAssessments.Count(a => a.Status == "Graded" && a.ScoredPoints.HasValue);
        int totalExpectedAssessments = course.ExpectedAssignmentCount + course.ExpectedMidtermCount + course.ExpectedFinalCount;
        if (totalExpectedAssessments <= 0)
        {
            totalExpectedAssessments = assignments.Count > 0 ? assignments.Count : 1;
        }

        decimal progressPercentage = totalExpectedAssessments > 0
            ? Math.Min(100m, Math.Round(((decimal)completedAssessments / totalExpectedAssessments) * 100m, 1))
            : 0m;

        if (completedAssessments < totalExpectedAssessments && progressPercentage >= 100m)
        {
            progressPercentage = Math.Min(99m, progressPercentage);
        }

        var progressSummary = new CourseProgressSummaryDto
        {
            CompletedAssessments = completedAssessments,
            TotalAssessments = totalExpectedAssessments,
            ProgressPercentage = progressPercentage,
            Status = (completedAssessments == totalExpectedAssessments && progressPercentage >= 100m) ? "Completed" : "In Progress"
        };

        // 5. Final Grade
        var finalGrade = new FinalGradeDto();
        if (enrollment.Grade.HasValue)
        {
            finalGrade.IsEvaluated = true;
            finalGrade.Status = "Evaluated";
            finalGrade.Score = enrollment.Grade.Value <= 4.0m ? enrollment.Grade.Value * 25m : enrollment.Grade.Value;
            finalGrade.Letter = _gradingService.CalculateGradeLevel(enrollment.Grade.Value <= 4.0m ? enrollment.Grade.Value * 25m : enrollment.Grade.Value).ToString();
            finalGrade.GradePoint = _gradingService.CalculateGradePoint(enrollment.Grade.Value <= 4.0m ? enrollment.Grade.Value * 25m : enrollment.Grade.Value);
        }
        else
        {
            finalGrade.IsEvaluated = false;
            finalGrade.Status = "Pending";
            finalGrade.Score = null;
            finalGrade.Letter = null;
            finalGrade.GradePoint = null;
        }

        // 6. Schedules
        var schedules = course.Schedules
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new CourseScheduleItemDto
            {
                Id = s.Id,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Location = s.Location,
                Room = s.Room,
                ScheduleType = s.ScheduleType
            }).ToList();

        var result = new StudentCourseProgressDetailDto
        {
            CourseSummary = courseSummary,
            Progress = progressSummary,
            Attendance = attendanceSummary,
            Assignments = assignmentSections,
            FinalGrade = finalGrade,
            Schedules = schedules
        };

        return Ok(result);
    }
}
