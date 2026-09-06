using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class UploadCourseThumbnailCommandValidator : AbstractValidator<UploadCourseThumbnailCommand>
{
    public UploadCourseThumbnailCommandValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
    }
}
