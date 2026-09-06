using FluentValidation;

namespace TmsApi.Application.Students.Commands;

public class UploadStudentAvatarCommandValidator : AbstractValidator<UploadStudentAvatarCommand>
{
    public UploadStudentAvatarCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
    }
}
