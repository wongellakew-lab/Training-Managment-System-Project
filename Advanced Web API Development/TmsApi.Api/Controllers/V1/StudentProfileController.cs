using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Students.Commands;
using TmsApi.Application.Students.Queries;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Students")]
public class StudentProfileController(
    IMediator _mediator,
    UserManager<TmsUser> _userManager)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(StudentProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the authenticated student's profile")]
    [EndpointDescription(
        "Returns the profile of the currently authenticated student, including identity details, " +
        "academic summary, roles, and course statistics. Returns 404 Not Found when the user is " +
        "not linked to a student profile."
    )]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound(new { message = "User not found." });

        var profile = await _mediator.Send(new GetStudentProfileQuery(user.Id), cancellationToken);
        if (profile == null) return NotFound(new { message = "Student profile not found." });

        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update the authenticated student's profile")]
    [EndpointDescription(
        "Updates the profile of the currently authenticated student. First name and last name are " +
        "required and trimmed before saving. Returns 400 Bad Request when required fields are " +
        "missing, 404 Not Found when the user has no linked student record, or 200 OK on success."
    )]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateStudentProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { message = "First name and Last name are required." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound(new { message = "User not found." });

        var dto = new UpdateStudentProfileDto
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Department = request.Department?.Trim(),
            Bio = request.Bio?.Trim()
        };

        var updated = await _mediator.Send(new UpdateStudentProfileCommand(user.Id, dto), cancellationToken);
        if (!updated) return NotFound(new { message = "Student not found." });

        return Ok(new { message = "Profile updated successfully." });
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Change the authenticated student's password")]
    [EndpointDescription(
        "Changes the password for the currently authenticated student. The new password must match " +
        "the confirmation field and satisfy the configured password policy. Returns 400 Bad Request " +
        "when the passwords do not match or the current password is incorrect."
    )]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeStudentPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return BadRequest(new { message = "New password and confirmation password do not match." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound(new { message = "User not found." });

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Password changed successfully." });
    }

    [HttpPost("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Upload the authenticated student's avatar")]
    [EndpointDescription(
        "Uploads a profile avatar for the currently authenticated student. Only JPEG, PNG, and WebP " +
        "images up to 2MB are accepted. Returns 400 Bad Request when no file is provided, the file " +
        "is too large, or its content type is not allowed."
    )]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size exceeds the 2MB limit." });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Only JPEG, PNG, and WebP images are permitted." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound(new { message = "User not found." });

        var avatarUrl = await _mediator.Send(new UploadStudentAvatarCommand(user.Id, file), cancellationToken);
        return Ok(new { avatarUrl, message = "Avatar uploaded successfully." });
    }
}
