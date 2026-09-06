using FluentValidation;

namespace TmsApi.Application.Assignments.Commands;

public class DeleteAssignmentCommandValidator : AbstractValidator<DeleteAssignmentCommand>
{
    public DeleteAssignmentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid assignment ID is required.");
    }
}
