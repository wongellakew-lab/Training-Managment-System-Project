using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class DeleteResourceCommandValidator : AbstractValidator<DeleteResourceCommand>
{
    public DeleteResourceCommandValidator()
    {
        RuleFor(x => x.ResourceId).GreaterThan(0).WithMessage("Valid resource ID is required.");
    }
}
