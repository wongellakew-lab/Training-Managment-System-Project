using System.Globalization;
using FluentValidation;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Validators;

public class CourseScheduleValidator : AbstractValidator<CourseScheduleDto>
{
    public CourseScheduleValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("A valid Day of Week is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.")
            .Must(BeValidTime).WithMessage("Start time must be a valid time format (e.g. 09:00 AM).");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required.")
            .Must(BeValidTime).WithMessage("End time must be a valid time format (e.g. 10:30 AM).");

        RuleFor(x => x)
            .Must(x => IsEndAfterStart(x.StartTime, x.EndTime))
            .WithMessage("End time must be after start time.")
            .When(x => BeValidTime(x.StartTime) && BeValidTime(x.EndTime));
    }

    private static bool BeValidTime(string timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr)) return false;
        string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
        return DateTime.TryParseExact(timeStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool IsEndAfterStart(string startStr, string endStr)
    {
        string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
        if (DateTime.TryParseExact(startStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
            DateTime.TryParseExact(endStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return end.TimeOfDay > start.TimeOfDay;
        }
        return true;
    }
}

public class CreateCourseScheduleDtoValidator : AbstractValidator<CreateCourseScheduleDto>
{
    public CreateCourseScheduleDtoValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("A valid Day of Week is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.")
            .Must(BeValidTime).WithMessage("Start time must be a valid time format (e.g. 09:00 AM).");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required.")
            .Must(BeValidTime).WithMessage("End time must be a valid time format (e.g. 10:30 AM).");

        RuleFor(x => x)
            .Must(x => IsEndAfterStart(x.StartTime, x.EndTime))
            .WithMessage("End time must be after start time.")
            .When(x => BeValidTime(x.StartTime) && BeValidTime(x.EndTime));
    }

    private static bool BeValidTime(string timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr)) return false;
        string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
        return DateTime.TryParseExact(timeStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool IsEndAfterStart(string startStr, string endStr)
    {
        string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
        if (DateTime.TryParseExact(startStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
            DateTime.TryParseExact(endStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return end.TimeOfDay > start.TimeOfDay;
        }
        return true;
    }
}

public static class CourseScheduleValidationHelper
{
    public static (bool IsValid, string? ErrorMessage) ValidateCourseDatesAndSchedules(
        DateTime startDate,
        DateTime endDate,
        int durationWeeks,
        IEnumerable<CourseScheduleDto>? schedules = null)
    {
        if (endDate < startDate)
        {
            return (false, "Course end date cannot be earlier than course start date.");
        }

        if (durationWeeks <= 0)
        {
            return (false, "Course duration in weeks must be greater than zero.");
        }

        if (schedules != null)
        {
            var validator = new CourseScheduleValidator();
            foreach (var sched in schedules)
            {
                var result = validator.Validate(sched);
                if (!result.IsValid)
                {
                    return (false, result.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid schedule configuration.");
                }
            }
        }

        return (true, null);
    }
}
