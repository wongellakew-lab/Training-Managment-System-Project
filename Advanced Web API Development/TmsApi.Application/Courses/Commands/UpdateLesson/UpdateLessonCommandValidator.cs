using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.LessonId).GreaterThan(0).WithMessage("Valid lesson ID is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Lesson title is required.");
    }
}
