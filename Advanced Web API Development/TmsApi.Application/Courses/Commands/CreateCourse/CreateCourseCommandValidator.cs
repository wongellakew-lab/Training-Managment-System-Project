using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).WithMessage("Course code is required and must not exceed 20 characters.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150).WithMessage("Course title is required.");
        RuleFor(x => x.MaxCapacity).GreaterThan(0).WithMessage("Maximum capacity must be greater than zero.");
    }
}

public class CreateCourseCommandValidator : CreateCourseValidator
{
}
