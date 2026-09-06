using FluentValidation;

namespace TmsApi.Application.Attendance.Commands;

public class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("Valid student ID is required.");
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Status).InclusiveBetween(1, 3).WithMessage("Attendance status must be Present (1), Late (2), or Absent (3).");
    }
}
