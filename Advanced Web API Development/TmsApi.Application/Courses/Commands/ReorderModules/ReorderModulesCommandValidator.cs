using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class ReorderModulesCommandValidator : AbstractValidator<ReorderModulesCommand>
{
    public ReorderModulesCommandValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Items).NotNull().WithMessage("Reorder items list cannot be null.");
    }
}
