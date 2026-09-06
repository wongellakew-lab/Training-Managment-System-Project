using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class UpdateModuleCommandValidator : AbstractValidator<UpdateModuleCommand>
{
    public UpdateModuleCommandValidator()
    {
        RuleFor(x => x.ModuleId).GreaterThan(0).WithMessage("Valid module ID is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Module title is required.");
    }
}
