using MediatR;
using TmsApi.Application.DTOs.AuthDtos;

namespace TmsApi.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshResponseDto?>;
