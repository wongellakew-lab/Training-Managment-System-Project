using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.NotificationDtos;

namespace TmsApi.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResponse<NotificationDto>> GetUserNotificationsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);
    Task<bool> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(string userId, string title, string message, string type = "System", int? relatedEntityId = null, string? relatedEntityType = null, CancellationToken cancellationToken = default);
}
