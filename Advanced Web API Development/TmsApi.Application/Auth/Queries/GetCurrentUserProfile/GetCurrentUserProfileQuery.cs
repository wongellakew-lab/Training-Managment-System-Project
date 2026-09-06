using System.Security.Claims;
using MediatR;
using TmsApi.Application.DTOs.AuthDtos;

namespace TmsApi.Application.Auth.Queries;

public record GetCurrentUserProfileQuery(ClaimsPrincipal UserPrincipal) : IRequest<AuthUserResponseDto?>;
