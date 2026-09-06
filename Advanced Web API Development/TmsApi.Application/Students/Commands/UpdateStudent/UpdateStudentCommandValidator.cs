using FluentValidation;

namespace TmsApi.Application.Students.Commands;

public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid student ID is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Update data is required.");
        When(x => x.Dto != null, () =>
        {
            RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("Last name is required.");
            RuleFor(x => x.Dto.Email).NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
        });
    }
}
