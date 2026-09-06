using MediatR;
using TmsApi.Application.DTOs.AuthDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Auth.Queries;

public class GetCurrentUserProfileQueryHandler(IAuthService _authService) : IRequestHandler<GetCurrentUserProfileQuery, AuthUserResponseDto?>
{
    public async Task<AuthUserResponseDto?> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        return await _authService.GetCurrentUserProfileAsync(request.UserPrincipal, cancellationToken);
    }
}
