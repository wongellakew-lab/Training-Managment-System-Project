using MediatR;
using TmsApi.Application.DTOs.AuthDtos;

namespace TmsApi.Application.Auth.Commands;

public record LoginCommand(LoginRequest Request) : IRequest<(bool Succeeded, AuthResponseDto? Response, string? ErrorMessage)>;
