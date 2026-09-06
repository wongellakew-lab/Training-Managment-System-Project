using FluentValidation;

namespace TmsApi.Application.Auth.Commands;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Registration data is required.");
        When(x => x.Request != null, () =>
        {
            RuleFor(x => x.Request.Email).NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(x => x.Request.Password).NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
            RuleFor(x => x.Request.FirstName).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.Request.LastName).NotEmpty().WithMessage("Last name is required.");
        });
    }
}
