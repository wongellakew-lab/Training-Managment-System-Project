using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Instructors.Commands;
using TmsApi.Application.Instructors.Queries;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Instructors")]
public class InstructorProfileController(
    IMediator _mediator,
    UserManager<TmsUser> _userManager)
    : ControllerBase
{
    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet]
    [ProducesResponseType(typeof(InstructorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the authenticated instructor's profile")]
    [EndpointDescription(
        "Returns the profile of the currently authenticated instructor, including identity details, " +
        "course, student, and certificate counts. Returns 404 Not Found when no instructor profile " +
        "is linked to the authenticated user."
    )]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var profile = await _mediator.Send(new GetInstructorProfileQuery(user.Id), cancellationToken);
        if (profile == null) return NotFound(new { message = "Instructor profile not found." });

        return Ok(profile);
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update the authenticated instructor's profile")]
    [EndpointDescription(
        "Updates the profile of the currently authenticated instructor, including name, contact " +
        "details, department, and bio. Returns 404 Not Found when no instructor record is linked " +
        "to the authenticated user."
    )]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateInstructorProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var dto = new UpdateInstructorProfileDto
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Department = request.Department,
            Bio = request.Bio
        };

        var success = await _mediator.Send(new UpdateInstructorProfileCommand(user.Id, dto), cancellationToken);
        if (!success) return NotFound(new { message = "Instructor not found." });

        return Ok(new
        {
            message = "Profile updated successfully.",
            firstName = request.FirstName,
            lastName = request.LastName,
            phoneNumber = request.PhoneNumber,
            department = request.Department
        });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Upload the authenticated instructor's avatar")]
    [EndpointDescription(
        "Uploads a profile avatar for the currently authenticated instructor. Only JPEG, PNG, and " +
        "WebP images up to 3MB are accepted. Returns 400 Bad Request when no file is provided, the " +
        "file is too large, or its content type is not allowed."
    )]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
        if (file.Length > 3 * 1024 * 1024) return BadRequest(new { message = "Avatar exceeds 3MB limit." });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Only JPEG, PNG, or WebP images are allowed." });

        var avatarUrl = await _mediator.Send(new UploadInstructorAvatarCommand(user.Id, file), cancellationToken);
        return Ok(new { profilePictureUrl = avatarUrl });
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Change the authenticated instructor's password")]
    [EndpointDescription(
        "Changes the password for the currently authenticated instructor. Returns 400 Bad Request " +
        "when the current password is incorrect or the new password does not satisfy the configured " +
        "password policy."
    )]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeInstructorPasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join("; ", result.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = "Password changed successfully." });
    }
}
