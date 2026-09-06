using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class ApproveCourseCommandValidator : AbstractValidator<ApproveCourseCommand>
{
    public ApproveCourseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid course ID is required.");
    }
}
