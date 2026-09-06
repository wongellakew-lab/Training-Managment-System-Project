using FluentValidation;

namespace TmsApi.Application.Auth.Commands;

public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
    }
}
