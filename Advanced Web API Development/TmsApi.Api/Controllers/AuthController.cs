using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Auth.Commands;
using TmsApi.Application.Auth.Queries;
using TmsApi.Application.DTOs.AuthDtos;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[Tags("Authentication")]
public class AuthController(
    IMediator _mediator)
    : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Register a new user account")]
    [EndpointDescription(
        "Creates a new Training Management System account for a student, instructor, or admin. " +
        "Returns the created user profile together with JWT access and refresh tokens. " +
        "The request is rejected with 400 Bad Request if the email is already registered or the " +
        "provided registration data is invalid."
    )]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var (succeeded, response, errors) = await _mediator.Send(new RegisterCommand(request), cancellationToken);
        if (!succeeded)
        {
            return BadRequest(errors);
        }

        return Ok(new
        {
            user = response!.User,
            token = response.Token,
            accessToken = response.AccessToken,
            refreshToken = response.RefreshToken
        });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Authenticate with email and password")]
    [EndpointDescription(
        "Validates the caller's credentials and returns the authenticated user profile together " +
        "with a JWT access token and a refresh token used to obtain new access tokens. " +
        "Returns 401 Unauthorized when the credentials are invalid or the account cannot sign in."
    )]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var (succeeded, response, errorMessage) = await _mediator.Send(new LoginCommand(request), cancellationToken);
        if (!succeeded)
        {
            return Unauthorized(new { message = errorMessage });
        }

        return Ok(new
        {
            user = response!.User,
            token = response.Token,
            accessToken = response.AccessToken,
            refreshToken = response.RefreshToken
        });
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Refresh an expired JWT access token")]
    [EndpointDescription(
        "Exchanges a valid refresh token for a new pair of JWT access and refresh tokens. " +
        "Does not change any database data. Returns 401 Unauthorized when the refresh token is " +
        "invalid, expired, or has already been revoked."
    )]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        return Ok(new
        {
            token = response.Token,
            accessToken = response.AccessToken,
            refreshToken = response.RefreshToken
        });
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointSummary("Sign out the current user")]
    [EndpointDescription(
        "Revokes the provided refresh token for the currently authenticated user so it can no " +
        "longer be used to obtain new access tokens. Requires no specific role beyond authentication."
    )]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        await _mediator.Send(new RevokeTokenCommand(request?.RefreshToken, userId), cancellationToken);
        return Ok(new { message = "Logged out successfully." });
    }

    [Authorize]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(AuthUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the current user's profile")]
    [EndpointDescription(
        "Returns the profile of the currently authenticated user, including identity details, " +
        "the assigned role, and the linked student or instructor ID when one exists. " +
        "Returns 404 Not Found if no user record exists for the authenticated principal."
    )]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetCurrentUserProfileQuery(User), cancellationToken);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.StudentId,
            user.InstructorId,
            role = user.Role,
            roles = user.Roles
        });
    }
}
