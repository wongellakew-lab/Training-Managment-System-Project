using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
{
    public DeleteCourseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid course ID is required.");
    }
}
