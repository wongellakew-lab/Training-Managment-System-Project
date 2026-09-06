using FluentValidation;

namespace TmsApi.Application.Instructors.Commands;

public class UpdateInstructorProfileCommandValidator : AbstractValidator<UpdateInstructorProfileCommand>
{
    public UpdateInstructorProfileCommandValidator()
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
