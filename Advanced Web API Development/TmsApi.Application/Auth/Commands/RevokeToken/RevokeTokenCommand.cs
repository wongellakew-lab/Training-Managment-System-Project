using MediatR;

namespace TmsApi.Application.Auth.Commands;

public record RevokeTokenCommand(string? RefreshToken, string? UserId) : IRequest<bool>;
