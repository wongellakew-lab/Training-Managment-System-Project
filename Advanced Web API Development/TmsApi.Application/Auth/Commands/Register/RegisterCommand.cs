using MediatR;
using TmsApi.Application.DTOs.AuthDtos;

namespace TmsApi.Application.Auth.Commands;

public record RegisterCommand(RegisterRequest Request) : IRequest<(bool Succeeded, AuthResponseDto? Response, object? Errors)>;
