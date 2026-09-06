using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class RejectCourseCommandValidator : AbstractValidator<RejectCourseCommand>
{
    public RejectCourseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Rejection reason cannot be empty.");
    }
}
