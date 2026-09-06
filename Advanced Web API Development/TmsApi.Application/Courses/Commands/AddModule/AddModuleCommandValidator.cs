using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class AddModuleCommandValidator : AbstractValidator<AddModuleCommand>
{
    public AddModuleCommandValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Module title is required.");
    }
}
