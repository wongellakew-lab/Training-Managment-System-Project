using FluentValidation;

namespace TmsApi.Application.Students.Commands;

public class UpdateStudentProfileCommandValidator : AbstractValidator<UpdateStudentProfileCommand>
{
    public UpdateStudentProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Profile data is required.");
        When(x => x.Dto != null, () =>
        {
            RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("Last name is required.");
        });
    }
}
