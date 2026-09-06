using System.Security.Claims;
using TmsApi.Application.DTOs.AuthDtos;

namespace TmsApi.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Succeeded, AuthResponseDto? Response, object? Errors)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, AuthResponseDto? Response, string? ErrorMessage)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<RefreshResponseDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string? refreshToken, string? userId, CancellationToken cancellationToken = default);
    Task<AuthUserResponseDto?> GetCurrentUserProfileAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default);
}
