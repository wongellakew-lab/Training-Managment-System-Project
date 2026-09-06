using FluentValidation;

namespace TmsApi.Application.Attendance.Commands;

public class SaveBulkAttendanceCommandValidator : AbstractValidator<SaveBulkAttendanceCommand>
{
    public SaveBulkAttendanceCommandValidator()
    {
        RuleFor(x => x.Request.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Request.Items).NotNull().WithMessage("Attendance items list cannot be null.");
    }
}
