using FluentValidation;

namespace TmsApi.Application.Instructors.Commands;

public class UploadInstructorAvatarCommandValidator : AbstractValidator<UploadInstructorAvatarCommand>
{
    public UploadInstructorAvatarCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
    }
}
