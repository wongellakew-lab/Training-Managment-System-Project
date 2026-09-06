using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class AddLessonCommandValidator : AbstractValidator<AddLessonCommand>
{
    public AddLessonCommandValidator()
    {
        RuleFor(x => x.ModuleId).GreaterThan(0).WithMessage("Valid module ID is required.");
        RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Lesson title is required.");
    }
}
