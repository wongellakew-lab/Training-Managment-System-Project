using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
{
    public DeleteLessonCommandValidator()
    {
        RuleFor(x => x.LessonId).GreaterThan(0).WithMessage("Valid lesson ID is required.");
    }
}
