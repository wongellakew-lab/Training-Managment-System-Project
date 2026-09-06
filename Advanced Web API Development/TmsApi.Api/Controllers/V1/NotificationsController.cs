using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.NotificationDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Notifications")]
public class NotificationsController(
    INotificationService _notificationService)
    : ControllerBase
{

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("List the authenticated user's notifications")]
    [EndpointDescription(
        "Returns the notifications for the currently authenticated user, optionally filtered to " +
        "unread items only, with paging. Returns 401 Unauthorized when the user identity cannot be " +
        "resolved."
    )]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize, unreadOnly, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Get the authenticated user's unread notification count")]
    [EndpointDescription(
        "Returns the number of unread notifications for the currently authenticated user. Returns " +
        "401 Unauthorized when the user identity cannot be resolved."
    )]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(new UnreadCountDto { Count = count });
    }

    [Authorize]
    [HttpPut("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Mark a single notification as read")]
    [EndpointDescription(
        "Marks the specified notification as read for the currently authenticated user. Returns " +
        "404 Not Found when the notification does not exist or does not belong to the user."
    )]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var success = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
        if (!success) return NotFound(new { message = "Notification not found or unauthorized." });

        return Ok(new { message = "Marked as read." });
    }

    [Authorize]
    [HttpPut("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Mark all of the authenticated user's notifications as read")]
    [EndpointDescription(
        "Marks every notification of the currently authenticated user as read and returns a " +
        "confirmation message."
    )]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(new { message = "All notifications marked as read." });
    }
}
