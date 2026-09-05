using System;
using System.Collections.Generic;

namespace TmsApi.Application.DTOs.CourseDtos;

public class StudentCourseProgressDetailDto
{
    public CourseSummaryDto CourseSummary { get; set; } = new();
    public CourseProgressSummaryDto Progress { get; set; } = new();
    public AttendanceSummaryDto Attendance { get; set; } = new();
    public AssignmentSectionsDto Assignments { get; set; } = new();
    public FinalGradeDto FinalGrade { get; set; } = new();
    public List<CourseScheduleItemDto> Schedules { get; set; } = new();
}

public class CourseSummaryDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Credits { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string EnrollmentStatus { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string InstructorEmail { get; set; } = string.Empty;
}

public class CourseProgressSummaryDto
{
    public decimal ProgressPercentage { get; set; }
    public int CompletedAssessments { get; set; }
    public int TotalAssessments { get; set; }
    public string Status { get; set; } = "In Progress";
}

public class AttendanceSummaryDto
{
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int TotalSessions { get; set; }
    public decimal AttendanceRate { get; set; }
    public List<AttendanceSessionDto> Sessions { get; set; } = new();
}

public class AttendanceSessionDto
{
    public int Id { get; set; }
    public DateTime SessionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class AssignmentSectionsDto
{
    public List<InProgressAssignmentDto> InProgress { get; set; } = new();
    public List<CompletedAssignmentDto> Completed { get; set; } = new();
}

public class InProgressAssignmentDto
{
    public int AssignmentId { get; set; }
    public int AssignmentNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal MaxPoints { get; set; }
    public string SubmissionStatus { get; set; } = "Not Submitted"; // "Not Submitted" or "Submitted"
    public DateTime? SubmittedAt { get; set; }
}

public class CompletedAssignmentDto
{
    public int AssignmentId { get; set; }
    public int AssignmentNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal MaxPoints { get; set; }
    public decimal ScoredPoints { get; set; }
    public DateTime? GradedAt { get; set; }
    public string? Feedback { get; set; }
}

public class FinalGradeDto
{
    public bool IsEvaluated { get; set; }
    public string Status { get; set; } = "Pending"; // "Evaluated" or "Pending"
    public decimal? Score { get; set; }
    public string? Letter { get; set; }
    public decimal? GradePoint { get; set; }
}

public class CourseScheduleItemDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "Lecture";
}
