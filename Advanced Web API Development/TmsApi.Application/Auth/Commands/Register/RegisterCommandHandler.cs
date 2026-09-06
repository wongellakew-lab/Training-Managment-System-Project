using MediatR;
using TmsApi.Application.DTOs.AuthDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Auth.Commands;

public class RegisterCommandHandler(IAuthService _authService) : IRequestHandler<RegisterCommand, (bool Succeeded, AuthResponseDto? Response, object? Errors)>
{
    public async Task<(bool Succeeded, AuthResponseDto? Response, object? Errors)> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RegisterAsync(request.Request, cancellationToken);
    }
}
