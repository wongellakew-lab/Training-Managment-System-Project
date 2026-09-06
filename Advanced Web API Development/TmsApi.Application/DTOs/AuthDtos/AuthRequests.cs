namespace TmsApi.Application.DTOs.AuthDtos;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string Role, string? RegNumber);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);

public record AuthUserResponseDto(
    string Id,
    string? Email,
    string FirstName,
    string LastName,
    int? StudentId,
    int? InstructorId,
    string Role,
    IList<string> Roles
);

public record AuthResponseDto(
    AuthUserResponseDto User,
    string Token,
    string AccessToken,
    string RefreshToken
);

public record RefreshResponseDto(
    string Token,
    string AccessToken,
    string RefreshToken
);
