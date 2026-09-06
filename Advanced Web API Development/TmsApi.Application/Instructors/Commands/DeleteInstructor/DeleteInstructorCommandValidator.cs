using FluentValidation;

namespace TmsApi.Application.Instructors.Commands;

public class DeleteInstructorCommandValidator : AbstractValidator<DeleteInstructorCommand>
{
    public DeleteInstructorCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid instructor ID is required.");
    }
}
