using FluentValidation;

namespace TmsApi.Application.Assignments.Commands;

public class UpdateAssignmentCommandValidator : AbstractValidator<UpdateAssignmentCommand>
{
    public UpdateAssignmentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid assignment ID is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Assignment title is required.");
        RuleFor(x => x.Dto.MaxPoints).GreaterThan(0).WithMessage("Max points must be greater than 0.");
    }
}
