using FluentValidation;

namespace TmsApi.Application.Instructors.Commands;

public class SuspendInstructorCommandValidator : AbstractValidator<SuspendInstructorCommand>
{
    public SuspendInstructorCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid instructor ID is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Suspend details are required.");
    }
}
