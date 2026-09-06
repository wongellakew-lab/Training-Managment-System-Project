using FluentValidation;

namespace TmsApi.Application.Students.Commands;

public class WithdrawFromCourseCommandValidator : AbstractValidator<WithdrawFromCourseCommand>
{
    public WithdrawFromCourseCommandValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("Valid student ID is required.");
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
    }
}
