using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class UploadCourseResourceCommandValidator : AbstractValidator<UploadCourseResourceCommand>
{
    public UploadCourseResourceCommandValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Resource title is required.");
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
    }
}
