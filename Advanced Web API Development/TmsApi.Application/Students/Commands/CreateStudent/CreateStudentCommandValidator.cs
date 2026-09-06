using FluentValidation;

namespace TmsApi.Application.Students.Commands;

public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(x => x.Dto).NotNull().WithMessage("Student data is required.");
        When(x => x.Dto != null, () =>
        {
            RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("Last name is required.");
            RuleFor(x => x.Dto.Email).NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
        });
    }
}
