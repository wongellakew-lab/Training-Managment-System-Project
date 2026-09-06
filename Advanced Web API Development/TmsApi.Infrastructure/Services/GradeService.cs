using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.GradeDtos;
using TmsApi.Application.Grading;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class GradeService(
    TmsDbContext _context,
    GradingService _gradingService,
    INotificationService _notificationService,
    ICourseService _courseService,
    IHubContext<TmsHub, ITmsClient> _hubContext,
    ICertificateService? _certificateService = null) : IGradeService
{

    public async Task<object?> GetStudentAcademicRecordAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student == null) return null;

        var enrollments = await _context.Enrollments
            .Include(e => e.Course)
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        var completed = enrollments.Where(e => e.Status == EnrollmentStatus.Completed || e.Grade.HasValue).ToList();
        var totalCredits = completed.Where(e => e.Course != null).Sum(e => e.Course!.Credits);

        var gradedEnrollments = enrollments.Where(e => e.Grade.HasValue && e.Course != null).ToList();
        decimal calculatedGpa = 0.0m;
        if (gradedEnrollments.Any())
        {
            var courseTuples = gradedEnrollments.Select(e => (e.Grade!.Value, e.Course?.Credits > 0 ? e.Course.Credits : 3));
            calculatedGpa = _gradingService.CalculateEnrollmentGPA(courseTuples);
        }
        else if (student.Gpa > 0)
        {
            calculatedGpa = _gradingService.CalculateGradePoint(student.Gpa);
        }

        var assessments = await _context.Assessments
            .AsNoTracking()
            .Include(a => a.Course)
            .Where(a => a.StudentId == studentId && a.IsPublished)
            .ToListAsync(cancellationToken);

        return new
        {
            studentId = student.Id,
            studentName = $"{student.FirstName} {student.LastName}",
            gpa = calculatedGpa,
            totalCredits,
            completedCoursesCount = completed.Count,
            grades = enrollments.Select(e =>
            {
                var courseAssessments = assessments.Where(a => a.CourseId == e.CourseId).Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    maxScore = a.MaxScore,
                    scoredPoints = a.ScoredPoints,
                    dueDate = a.DueDate,
                    status = a.Status,
                    feedback = a.Feedback,
                    gradedAt = a.GradedAt,
                    rubricScoresJson = a.RubricScoresJson
                }).ToList();

                return new
                {
                    enrollmentId = e.Id,
                    courseId = e.CourseId,
                    courseCode = e.Course?.Code ?? string.Empty,
                    courseTitle = e.Course?.Title ?? string.Empty,
                    credits = e.Course?.Credits ?? 3,
                    numericScore = e.Grade.HasValue ? (e.Grade.Value <= 4.0m ? e.Grade.Value * 25 : e.Grade.Value) : (decimal?)null,
                    letterGrade = e.Grade.HasValue ? _gradingService.CalculateGradeLevel(e.Grade.Value).ToString() : null,
                    status = e.Status.ToString(),
                    assessments = courseAssessments
                };
            })
        };
    }

    public async Task<object?> AssignGradeAsync(AssignGradeRequestDto dto, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId, cancellationToken);

        if (enrollment == null) return null;

        decimal numericScore = dto.NumericScore ?? dto.Grade ?? 85m;
        decimal gpaValue = numericScore > 4.0m ? (numericScore / 100m) * 4.0m : numericScore;

        enrollment.Grade = gpaValue;
        if (gpaValue >= 2.0m)
        {
            enrollment.Status = EnrollmentStatus.Completed;
        }

        var studentGrades = await _context.Enrollments
            .Where(e => e.StudentId == enrollment.StudentId && e.Grade.HasValue && e.Id != enrollment.Id)
            .Select(e => e.Grade!.Value)
            .ToListAsync(cancellationToken);

        studentGrades.Add(gpaValue);
        enrollment.Student.Gpa = _gradingService.CalculateGPA(studentGrades);

        await _context.SaveChangesAsync(cancellationToken);

        var letterGrade = _gradingService.CalculateGradeLevel(gpaValue).ToString();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentId == enrollment.StudentId, cancellationToken);
        var notifUserId = user?.Id ?? enrollment.Student.UserId ?? enrollment.StudentId.ToString();

        await _notificationService.CreateNotificationAsync(
            notifUserId,
            "Grade Published",
            $"Your final grade for {enrollment.Course?.Code ?? "Course"} ({enrollment.Course?.Title}) is {letterGrade} ({numericScore:F0}%).",
            "Grade",
            enrollment.Id,
            "Enrollment",
            cancellationToken);

        await _hubContext.Clients.Group($"Student_{enrollment.StudentId}").ReceiveNotification(
            "Grade Published",
            $"Your final grade for {enrollment.Course?.Code ?? "Course"} ({enrollment.Course?.Title}) is {letterGrade} ({numericScore:F0}%).");
        await _hubContext.Clients.Group($"Student_{enrollment.StudentId}").ReceiveEnrollmentNotification(new
        {
            studentId = enrollment.StudentId,
            courseId = enrollment.CourseId,
            grade = gpaValue,
            letterGrade
        });
        await _hubContext.Clients.Group($"Student_{enrollment.StudentId}").ReceiveGradePublished(new
        {
            studentId = enrollment.StudentId,
            courseId = enrollment.CourseId,
            grade = gpaValue,
            letterGrade
        });

        if (user != null)
        {
            await _hubContext.Clients.Group($"User_{user.Id}").ReceiveNotification(
                "Grade Published",
                $"Your final grade for {enrollment.Course?.Code ?? "Course"} ({enrollment.Course?.Title}) is {letterGrade} ({numericScore:F0}%).");
        }

        if (_certificateService != null)
        {
            await _certificateService.CheckAndIssueCertificateIfEligibleAsync(enrollment.StudentId, enrollment.CourseId, cancellationToken);
        }

        return new
        {
            enrollmentId = enrollment.Id,
            grade = gpaValue,
            numericScore,
            updatedGpa = enrollment.Student.Gpa,
            letterGrade
        };
    }

    public async Task<List<object>> BulkAssignGradesAsync(BulkAssignGradesRequestDto request, CancellationToken cancellationToken = default)
    {
        var results = new List<object>();
        var affectedStudentIds = new HashSet<int>();

        foreach (var item in request.Grades)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == item.EnrollmentId, cancellationToken);

            if (enrollment != null)
            {
                decimal numericScore = item.NumericScore ?? item.Grade ?? 85m;
                decimal gpaValue = numericScore > 4.0m ? (numericScore / 100m) * 4.0m : numericScore;
                enrollment.Grade = gpaValue;
                if (gpaValue >= 2.0m) enrollment.Status = EnrollmentStatus.Completed;

                affectedStudentIds.Add(enrollment.StudentId);

                var letterGrade = _gradingService.CalculateGradeLevel(gpaValue).ToString();
                results.Add(new { enrollmentId = item.EnrollmentId, grade = gpaValue, letterGrade, status = "Updated" });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentId == enrollment.StudentId, cancellationToken);
                if (user != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        "Grade Published",
                        $"Your grade for {enrollment.Course?.Code ?? "Course"} is {letterGrade}.",
                        "Grade",
                        enrollment.Id,
                        "Enrollment",
                        cancellationToken);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var sId in affectedStudentIds)
        {
            var sGrades = await _context.Enrollments
                .Where(e => e.StudentId == sId && e.Grade.HasValue)
                .Select(e => e.Grade!.Value)
                .ToListAsync(cancellationToken);

            var student = await _context.Students.FindAsync(new object[] { sId }, cancellationToken);
            if (student != null)
            {
                student.Gpa = _gradingService.CalculateGPA(sGrades);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (_certificateService != null)
        {
            var enrollmentList = await _context.Enrollments
                .AsNoTracking()
                .Where(e => request.Grades.Select(g => g.EnrollmentId).Contains(e.Id))
                .Select(e => new { e.StudentId, e.CourseId })
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var en in enrollmentList)
            {
                await _certificateService.CheckAndIssueCertificateIfEligibleAsync(en.StudentId, en.CourseId, cancellationToken);
            }
        }

        return results;
    }

    public async Task<object?> GetCourseGradebookAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, cancellationToken);

        if (course == null) return null;

        if (course.Status != "Active" && course.Status != "Completed")
        {
            return new
            {
                courseId = course.Id,
                courseTitle = course.Title,
                courseCode = course.Code,
                courseStatus = course.Status,
                isApproved = false,
                message = "Course is pending approval. Assessments and gradebook will be automatically generated once approved by Admin.",
                assignments = new List<object>(),
                rows = new List<object>(),
                alreadyEvaluatedCount = 0,
                notYetEvaluatedCount = 0,
                totalRequiredEvaluations = 0
            };
        }

        await _courseService.EnsureCourseAssessmentsGeneratedAsync(courseId, cancellationToken);

        var assignments = await _context.Assignments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && !a.IsDeleted && a.IsPublished)
            .OrderBy(a => a.AssignmentNumber)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new
            {
                id = a.Id,
                assignmentNumber = a.AssignmentNumber,
                title = a.Title,
                maxPoints = a.MaxPoints,
                dueDate = a.DueDate
            })
            .ToListAsync(cancellationToken);

        var assignmentIds = assignments.Select(a => a.id).ToList();

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
                .ThenInclude(s => s.User)
            .Where(e => e.CourseId == courseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .OrderBy(e => e.Student.LastName)
            .ThenBy(e => e.Student.FirstName)
            .ToListAsync(cancellationToken);

        var assessments = await _context.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && a.AssignmentId.HasValue && assignmentIds.Contains(a.AssignmentId.Value))
            .ToListAsync(cancellationToken);

        var rows = enrollments.Select(e =>
        {
            var st = e.Student;
            var studentAssessments = assessments.Where(a => a.StudentId == st.Id).ToList();

            var gradeCells = assignments.Select(asgn =>
            {
                var match = studentAssessments.FirstOrDefault(a => a.AssignmentId == asgn.id);
                return new
                {
                    assignmentId = asgn.id,
                    assessmentId = match?.Id,
                    score = match?.ScoredPoints,
                    maxScore = match?.MaxScore ?? asgn.maxPoints,
                    status = match?.Status ?? "Pending",
                    isGraded = match != null && match.Status == "Graded" && match.ScoredPoints.HasValue,
                    feedback = match?.Feedback
                };
            }).ToList();

            var requiredCount = assignments.Count;
            var gradedCells = gradeCells.Where(g => g.isGraded && g.score.HasValue).ToList();
            var gradedCount = gradedCells.Count;
            var isGradingComplete = requiredCount > 0 && gradedCount == requiredCount;

            decimal? totalScore = isGradingComplete ? gradedCells.Sum(g => g.score!.Value) : null;
            decimal? totalMaxScore = isGradingComplete ? assignments.Sum(a => a.maxPoints) : null;
            decimal? avgPercentage = isGradingComplete && totalMaxScore > 0 ? Math.Round((totalScore!.Value / totalMaxScore.Value) * 100m, 1) : null;
            string letter = isGradingComplete && avgPercentage.HasValue ? _gradingService.CalculateGradeLevel(avgPercentage.Value).ToString() : "Pending";

            return new
            {
                studentId = st.Id,
                studentName = $"{st.FirstName} {st.LastName}",
                registrationNumber = st.RegistrationNumber,
                avatarUrl = st.User?.ProfilePictureUrl ?? st.ProfilePictureUrl,
                grades = gradeCells,
                totalScore,
                totalMaxScore,
                averagePercentage = avgPercentage,
                letterGrade = letter,
                isGradingComplete,
                gradedAssessmentCount = gradedCount,
                requiredAssessmentCount = requiredCount
            };
        }).ToList();

        var totalRequiredEvaluations = enrollments.Count * assignments.Count;
        var alreadyEvaluatedCount = rows.Sum(r => r.grades.Count(g => g.isGraded && g.score.HasValue));
        var notYetEvaluatedCount = Math.Max(0, totalRequiredEvaluations - alreadyEvaluatedCount);

        return new
        {
            courseId = course.Id,
            courseTitle = course.Title,
            courseCode = course.Code,
            courseStatus = course.Status,
            isApproved = true,
            assignments,
            rows,
            alreadyEvaluatedCount,
            notYetEvaluatedCount,
            totalRequiredEvaluations
        };
    }

    public async Task<object?> SubmitBulkGradebookAsync(BulkGradebookSubmitRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Grades == null || !request.Grades.Any())
        {
            return new { success = false, message = "No grades submitted." };
        }

        var assignmentIds = request.Grades.Select(g => g.AssignmentId).Distinct().ToList();
        var assignments = await _context.Assignments
            .Where(a => a.CourseId == request.CourseId && assignmentIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        foreach (var item in request.Grades)
        {
            if (!assignments.TryGetValue(item.AssignmentId, out var assignment))
            {
                return new { success = false, message = $"Assignment with ID {item.AssignmentId} not found in this course." };
            }

            if (!item.Score.HasValue)
            {
                return new { success = false, message = "Grade is required for all items." };
            }

            if (item.Score.Value < 0 || item.Score.Value > assignment.MaxPoints)
            {
                return new { success = false, message = $"Grade out of bounds for assignment '{assignment.Title}' (0 - {assignment.MaxPoints})." };
            }
        }

        var studentIds = request.Grades.Select(g => g.StudentId).Distinct().ToList();
        var existingAssessments = await _context.Assessments
            .Where(a => a.CourseId == request.CourseId &&
                        studentIds.Contains(a.StudentId) &&
                        assignmentIds.Contains(a.AssignmentId!.Value))
            .ToListAsync(cancellationToken);

        var affectedStudentIds = new HashSet<int>();

        foreach (var item in request.Grades)
        {
            var assignment = assignments[item.AssignmentId];
            var assessment = existingAssessments.FirstOrDefault(a => a.StudentId == item.StudentId && a.AssignmentId == item.AssignmentId);

            if (assessment == null)
            {
                assessment = new Assessment
                {
                    CourseId = request.CourseId,
                    StudentId = item.StudentId,
                    AssignmentId = item.AssignmentId,
                    Title = assignment.Title,
                    MaxScore = assignment.MaxPoints,
                    DueDate = assignment.DueDate,
                    IsPublished = true
                };
                await _context.Assessments.AddAsync(assessment, cancellationToken);
                existingAssessments.Add(assessment);
            }

            if (item.Score.HasValue)
            {
                assessment.ScoredPoints = item.Score.Value;
                assessment.Status = "Graded";
                assessment.GradedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(item.Feedback)) assessment.Feedback = item.Feedback;
                affectedStudentIds.Add(item.StudentId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (affectedStudentIds.Any())
        {
            var enrollments = await _context.Enrollments
                .Where(e => e.CourseId == request.CourseId && affectedStudentIds.Contains(e.StudentId) && !e.IsDeleted)
                .ToListAsync(cancellationToken);

            var totalAssignmentsCount = await _context.Assignments
                .CountAsync(a => a.CourseId == request.CourseId && !a.IsDeleted && a.IsPublished, cancellationToken);

            var allStudentAssessments = await _context.Assessments
                .Where(a => a.CourseId == request.CourseId && affectedStudentIds.Contains(a.StudentId))
                .ToListAsync(cancellationToken);

            foreach (var en in enrollments)
            {
                var sAssessments = allStudentAssessments.Where(a => a.StudentId == en.StudentId).ToList();
                var gradedAssessments = sAssessments.Where(a => a.ScoredPoints.HasValue).ToList();

                if (gradedAssessments.Any())
                {
                    decimal totalScored = gradedAssessments.Sum(a => a.ScoredPoints!.Value);
                    decimal totalMax = gradedAssessments.Sum(a => a.MaxScore);
                    decimal avgPct = totalMax > 0 ? Math.Round((totalScored / totalMax) * 100m, 1) : 0m;
                    en.Grade = avgPct;

                    if (totalAssignmentsCount > 0 && gradedAssessments.Count >= totalAssignmentsCount && avgPct >= 60m)
                    {
                        en.Status = EnrollmentStatus.Completed;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (_certificateService != null)
            {
                foreach (var sId in affectedStudentIds)
                {
                    await _certificateService.CheckAndIssueCertificateIfEligibleAsync(sId, request.CourseId, cancellationToken);
                }
            }
        }

        foreach (var sId in affectedStudentIds)
        {
            await _hubContext.Clients.Group($"Student_{sId}").ReceiveGradePublished(new
            {
                courseId = request.CourseId,
                studentId = sId,
                message = "Grades have been updated."
            });
        }
        await _hubContext.Clients.All.ReceiveScheduleUpdate(new { action = "gradesSubmitted", courseId = request.CourseId });

        var bulkStats = await GetCourseEvaluationStatsAsync(request.CourseId, cancellationToken);

        return new
        {
            success = true,
            message = "Grades submitted successfully",
            updatedCount = request.Grades.Count,
            alreadyEvaluatedCount = bulkStats.alreadyEvaluated,
            notYetEvaluatedCount = bulkStats.notYetEvaluated,
            totalRequiredEvaluations = bulkStats.totalRequired
        };
    }

    public async Task<object?> UpdateGradebookCellAsync(GradebookCellRequestDto request, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.Assignments.FindAsync(new object[] { request.AssignmentId }, cancellationToken);
        if (assignment == null) return null;

        if (!request.Score.HasValue || request.Score.Value < 0 || request.Score.Value > assignment.MaxPoints)
        {
            return new { success = false, message = "Invalid score range." };
        }

        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.CourseId == request.CourseId &&
                                      a.StudentId == request.StudentId &&
                                      a.AssignmentId == request.AssignmentId, cancellationToken);

        if (assessment == null)
        {
            assessment = new Assessment
            {
                CourseId = request.CourseId,
                StudentId = request.StudentId,
                AssignmentId = request.AssignmentId,
                Title = assignment.Title,
                MaxScore = assignment.MaxPoints,
                DueDate = assignment.DueDate,
                IsPublished = true
            };
            await _context.Assessments.AddAsync(assessment, cancellationToken);
        }

        assessment.ScoredPoints = request.Score.Value;
        assessment.Status = "Graded";
        assessment.GradedAt = DateTime.UtcNow;
        if (request.Feedback != null) assessment.Feedback = request.Feedback;

        await _context.SaveChangesAsync(cancellationToken);

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.StudentId == request.StudentId && !e.IsDeleted, cancellationToken);

        if (enrollment != null)
        {
            var totalAssignmentsCount = await _context.Assignments
                .CountAsync(a => a.CourseId == request.CourseId && !a.IsDeleted && a.IsPublished, cancellationToken);

            var studentAssessments = await _context.Assessments
                .Where(a => a.CourseId == request.CourseId && a.StudentId == request.StudentId)
                .ToListAsync(cancellationToken);

            var gradedAssessments = studentAssessments.Where(a => a.ScoredPoints.HasValue).ToList();
            if (gradedAssessments.Any())
            {
                decimal totalScored = gradedAssessments.Sum(a => a.ScoredPoints!.Value);
                decimal totalMax = gradedAssessments.Sum(a => a.MaxScore);
                decimal avgPct = totalMax > 0 ? Math.Round((totalScored / totalMax) * 100m, 1) : 0m;
                enrollment.Grade = avgPct;

                if (totalAssignmentsCount > 0 && gradedAssessments.Count >= totalAssignmentsCount && avgPct >= 60m)
                {
                    enrollment.Status = EnrollmentStatus.Completed;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (_certificateService != null)
            {
                await _certificateService.CheckAndIssueCertificateIfEligibleAsync(request.StudentId, request.CourseId, cancellationToken);
            }
        }

        await _hubContext.Clients.Group($"Student_{request.StudentId}").ReceiveGradePublished(new
        {
            courseId = request.CourseId,
            studentId = request.StudentId,
            assignmentId = request.AssignmentId,
            scoredPoints = request.Score.Value,
            maxScore = assignment.MaxPoints,
            message = $"Grade published for '{assignment.Title}'."
        });

        var cellStats = await GetCourseEvaluationStatsAsync(request.CourseId, cancellationToken);

        return new
        {
            success = true,
            message = "Grade updated successfully.",
            assessmentId = assessment.Id,
            studentId = assessment.StudentId,
            assignmentId = assessment.AssignmentId,
            score = assessment.ScoredPoints,
            status = assessment.Status,
            alreadyEvaluatedCount = cellStats.alreadyEvaluated,
            notYetEvaluatedCount = cellStats.notYetEvaluated,
            totalRequiredEvaluations = cellStats.totalRequired
        };
    }

    private async Task<(int alreadyEvaluated, int notYetEvaluated, int totalRequired)> GetCourseEvaluationStatsAsync(int courseId, CancellationToken cancellationToken)
    {
        var enrolledStudentIds = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed) && !e.IsDeleted)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);

        var publishedAssignmentIds = await _context.Assignments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && !a.IsDeleted && a.IsPublished)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var totalRequired = enrolledStudentIds.Count * publishedAssignmentIds.Count;

        var alreadyEvaluated = await _context.Assessments
            .AsNoTracking()
            .CountAsync(a => a.CourseId == courseId &&
                             a.AssignmentId.HasValue &&
                             publishedAssignmentIds.Contains(a.AssignmentId.Value) &&
                             enrolledStudentIds.Contains(a.StudentId) &&
                             a.Status == "Graded" &&
                             a.ScoredPoints.HasValue, cancellationToken);

        var notYetEvaluated = Math.Max(0, totalRequired - alreadyEvaluated);

        return (alreadyEvaluated, notYetEvaluated, totalRequired);
    }
}
