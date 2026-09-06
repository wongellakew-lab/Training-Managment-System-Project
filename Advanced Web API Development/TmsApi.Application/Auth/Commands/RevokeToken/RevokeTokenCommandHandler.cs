using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Auth.Commands;

public class RevokeTokenCommandHandler(IAuthService _authService) : IRequestHandler<RevokeTokenCommand, bool>
{
    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken, request.UserId, cancellationToken);
        return true;
    }
}
