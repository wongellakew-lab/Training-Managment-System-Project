using FluentValidation;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Validators;

public class CreateCourseDtoValidator : AbstractValidator<CreateCourseDto>
{
    public CreateCourseDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Course name is required.")
            .MinimumLength(3).WithMessage("Course name must be at least 3 characters.")
            .MaximumLength(200).WithMessage("Course name cannot exceed 200 characters.");

        RuleFor(x => x.DurationHours)
            .GreaterThanOrEqualTo(0).WithMessage("Duration cannot be negative.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThanOrEqualTo(0).WithMessage("Capacity cannot be negative.");

        RuleFor(x => x.MaxCapacity)
            .NotEqual(0).WithMessage("Capacity must be greater than 0.");

        RuleFor(x => x.ExpectedAssignmentCount)
            .GreaterThanOrEqualTo(0).WithMessage("Assignment count cannot be negative.");

        RuleFor(x => x.ExpectedMidtermCount)
            .GreaterThanOrEqualTo(0).WithMessage("Midterm exam count cannot be negative.");

        RuleFor(x => x.ExpectedFinalCount)
            .GreaterThanOrEqualTo(0).WithMessage("Final exam count cannot be negative.");

        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be on or after the start date.");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Start date cannot be after the end date.");
        });

        When(x => x.Status == "Active" || x.Status == "Published" || x.Status == "Under Review", () =>
        {
            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Course category is required to submit.");

            RuleFor(x => x.Level)
                .NotEmpty().WithMessage("Skill level is required to submit.");

            RuleFor(x => x.DurationHours)
                .GreaterThan(0).WithMessage("Duration must be greater than 0 hours to submit.");

            RuleFor(x => x.MaxCapacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.About) || !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Course overview or description is required to submit.")
                .WithName("About");

            RuleFor(x => x)
                .Must(x => x.Modules != null && x.Modules.Count > 0 && x.Modules.Any(m => m.Lessons != null && m.Lessons.Count > 0))
                .WithMessage("Course content is required. Add at least one content item before publishing.")
                .WithName("CourseContent");

            RuleFor(x => x.Schedules)
                .Must(s => s != null && s.Count > 0)
                .WithMessage("At least one weekly schedule is required before submitting the course.")
                .WithName("schedules");

            RuleForEach(x => x.Schedules)
                .SetValidator(new CreateCourseScheduleDtoValidator());

            RuleFor(x => x.Schedules)
                .Must(HaveNoDuplicateSchedules)
                .WithMessage("Schedule contains duplicate or overlapping entries for the same day and time.")
                .WithName("schedules")
                .When(x => x.Schedules != null && x.Schedules.Count > 1);

            RuleFor(x => x)
                .Must(x => (x.ExpectedAssignmentCount + x.ExpectedMidtermCount + x.ExpectedFinalCount) >= 1)
                .WithMessage("At least one assessable item (Assignment, Midterm, or Final Exam) is required for course completion.")
                .WithName("assessmentStructure");
        });
    }

    private static bool HaveNoDuplicateSchedules(List<CreateCourseScheduleDto>? schedules)
    {
        if (schedules == null || schedules.Count <= 1) return true;
        string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
        for (int i = 0; i < schedules.Count; i++)
        {
            var s1 = schedules[i];
            if (!DateTime.TryParseExact(s1.StartTime?.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var start1) ||
                !DateTime.TryParseExact(s1.EndTime?.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var end1))
                continue;

            for (int j = i + 1; j < schedules.Count; j++)
            {
                var s2 = schedules[j];
                if (s1.DayOfWeek != s2.DayOfWeek) continue;
                if (!DateTime.TryParseExact(s2.StartTime?.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var start2) ||
                    !DateTime.TryParseExact(s2.EndTime?.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var end2))
                    continue;

                if (start1.TimeOfDay < end2.TimeOfDay && start2.TimeOfDay < end1.TimeOfDay)
                {
                    return false;
                }
            }
        }
        return true;
    }
}

public class UpdateCourseDtoValidator : AbstractValidator<UpdateCourseDto>
{
    public UpdateCourseDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Course name is required.")
            .MinimumLength(3).WithMessage("Course name must be at least 3 characters.")
            .MaximumLength(200).WithMessage("Course name cannot exceed 200 characters.");

        // When saving as Draft, allow partial information
        RuleFor(x => x.DurationHours)
            .GreaterThanOrEqualTo(0).WithMessage("Duration cannot be negative.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThanOrEqualTo(0).WithMessage("Capacity cannot be negative.");

        RuleFor(x => x.MaxCapacity)
            .NotEqual(0).WithMessage("Capacity must be greater than 0.");

        RuleFor(x => x.ExpectedAssignmentCount)
            .GreaterThanOrEqualTo(0).WithMessage("Assignment count cannot be negative.");

        RuleFor(x => x.ExpectedMidtermCount)
            .GreaterThanOrEqualTo(0).WithMessage("Midterm exam count cannot be negative.");

        RuleFor(x => x.ExpectedFinalCount)
            .GreaterThanOrEqualTo(0).WithMessage("Final exam count cannot be negative.");

        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be on or after the start date.");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("Start date cannot be after the end date.");
        });

        // When publishing as Active / Published or submitting as Under Review, enforce full business requirements
        When(x => x.Status == "Active" || x.Status == "Published" || x.Status == "Under Review", () =>
        {
            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Course category is required to submit.");

            RuleFor(x => x.Level)
                .NotEmpty().WithMessage("Skill level is required to submit.");

            RuleFor(x => x.DurationHours)
                .GreaterThan(0).WithMessage("Duration must be greater than 0 hours to submit.");

            RuleFor(x => x.MaxCapacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.About) || !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Course overview or description is required to submit.")
                .WithName("About");

            RuleFor(x => x)
                .Must(x => x.Modules != null && x.Modules.Count > 0 && x.Modules.Any(m => m.Lessons != null && m.Lessons.Count > 0))
                .WithMessage("Course content is required. Add at least one content item before publishing.")
                .WithName("CourseContent")
                .When(x => x.Modules != null && x.Modules.Count > 0);

            RuleFor(x => x.Schedules)
                .Must(s => s != null && s.Count > 0)
                .WithMessage("At least one weekly schedule is required before submitting the course.")
                .WithName("schedules");

            RuleForEach(x => x.Schedules)
                .SetValidator(new CreateCourseScheduleDtoValidator());

            RuleFor(x => x.Schedules)
                .Must(HaveNoDuplicateSchedules)
                .WithMessage("Schedule contains duplicate or overlapping entries for the same day and time.")
                .WithName("schedules")
                .When(x => x.Schedules != null && x.Schedules.Count > 1);

            RuleFor(x => x)
                .Must(x => (x.ExpectedAssignmentCount + x.ExpectedMidtermCount + x.ExpectedFinalCount) >= 1)
                .WithMessage("At least one assessable item (Assignment, Midterm, or Final Exam) is required for course completion.")
                .WithName("assessmentStructure");
        });
    }

    private static bool HaveNoDuplicateSchedules(List<CreateCourseScheduleDto>? schedules)
    {
        if (schedules == null || schedules.Count <= 1) return true;
        string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
        for (int i = 0; i < schedules.Count; i++)
        {
            var s1 = schedules[i];
            if (!DateTime.TryParseExact(s1.StartTime?.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var start1) ||
                !DateTime.TryParseExact(s1.EndTime?.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var end1))
                continue;

            for (int j = i + 1; j < schedules.Count; j++)
            {
                var s2 = schedules[j];
                if (s1.DayOfWeek != s2.DayOfWeek) continue;
                if (!DateTime.TryParseExact(s2.StartTime?.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var start2) ||
                    !DateTime.TryParseExact(s2.EndTime?.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var end2))
                    continue;

                if (start1.TimeOfDay < end2.TimeOfDay && start2.TimeOfDay < end1.TimeOfDay)
                {
                    return false;
                }
            }
        }
        return true;
    }
}

