using FluentValidation;

namespace TmsApi.Application.Assignments.Commands;

public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentCommandValidator()
    {
        RuleFor(x => x.Dto.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Assignment title is required.");
        RuleFor(x => x.Dto.MaxPoints).GreaterThan(0).WithMessage("Max points must be greater than 0.");
    }
}
