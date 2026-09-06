using MediatR;
using TmsApi.Application.DTOs.AuthDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Auth.Commands;

public class RefreshTokenCommandHandler(IAuthService _authService) : IRequestHandler<RefreshTokenCommand, RefreshResponseDto?>
{
    public async Task<RefreshResponseDto?> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
    }
}
