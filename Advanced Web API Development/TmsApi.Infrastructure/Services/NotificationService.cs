using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.NotificationDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHubContext<TmsHub> _hubContext;

    public NotificationService(TmsDbContext context, IMapper mapper, IHubContext<TmsHub> hubContext)
    {
        _context = context;
        _mapper = mapper;
        _hubContext = hubContext;
    }

    public async Task<PagedResponse<NotificationDto>> GetUserNotificationsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<NotificationDto>(_mapper.Map<IEnumerable<NotificationDto>>(items), page, pageSize, total);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification == null) return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0) return true;

        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<NotificationDto> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        string type = "System",
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType
        };

        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast notification via SignalR
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", title, message, cancellationToken);
        }
        catch
        {
            // Suppress if no active clients
        }

        return _mapper.Map<NotificationDto>(notification);
    }
}
