using MediatR;
using TmsApi.Application.DTOs.AuthDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Auth.Commands;

public class LoginCommandHandler(IAuthService _authService) : IRequestHandler<LoginCommand, (bool Succeeded, AuthResponseDto? Response, string? ErrorMessage)>
{
    public async Task<(bool Succeeded, AuthResponseDto? Response, string? ErrorMessage)> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(request.Request, cancellationToken);
    }
}
