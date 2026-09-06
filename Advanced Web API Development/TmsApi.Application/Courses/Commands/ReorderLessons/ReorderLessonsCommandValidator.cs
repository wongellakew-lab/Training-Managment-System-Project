using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class ReorderLessonsCommandValidator : AbstractValidator<ReorderLessonsCommand>
{
    public ReorderLessonsCommandValidator()
    {
        RuleFor(x => x.ModuleId).GreaterThan(0).WithMessage("Valid module ID is required.");
        RuleFor(x => x.Items).NotNull().WithMessage("Reorder items list cannot be null.");
    }
}
