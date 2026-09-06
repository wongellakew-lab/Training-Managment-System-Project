using FluentValidation;

namespace TmsApi.Application.Auth.Commands;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Login credentials are required.");
        When(x => x.Request != null, () =>
        {
            RuleFor(x => x.Request.Email).NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(x => x.Request.Password).NotEmpty().WithMessage("Password is required.");
        });
    }
}
