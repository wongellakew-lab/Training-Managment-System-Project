using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Course details are required.");
    }
}
