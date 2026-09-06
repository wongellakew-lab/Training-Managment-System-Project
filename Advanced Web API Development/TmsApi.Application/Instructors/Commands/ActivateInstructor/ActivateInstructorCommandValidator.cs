using FluentValidation;

namespace TmsApi.Application.Instructors.Commands;

public class ActivateInstructorCommandValidator : AbstractValidator<ActivateInstructorCommand>
{
    public ActivateInstructorCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid instructor ID is required.");
    }
}
