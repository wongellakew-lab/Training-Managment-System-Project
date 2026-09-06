using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class DeleteModuleCommandValidator : AbstractValidator<DeleteModuleCommand>
{
    public DeleteModuleCommandValidator()
    {
        RuleFor(x => x.ModuleId).GreaterThan(0).WithMessage("Valid module ID is required.");
    }
}
