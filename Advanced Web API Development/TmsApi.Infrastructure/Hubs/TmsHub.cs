using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TmsApi.Infrastructure.Hubs;

public interface ITmsClient
{
    Task ReceiveEnrollmentUpdate(string message, object data);
    Task ReceiveNotification(string title, string message);
    Task ReceiveTranscriptStatus(string requestId, string status, string? downloadUrl);
    Task ReceiveEnrollmentNotification(object data);
    Task ReceiveCapacityAlert(object data);
    Task ReceiveScheduleUpdate(object? data = null);
    Task ReceiveGradePublished(object data);
    Task ReceiveAttendanceUpdate(object data);
    Task ReceiveAssignmentUpdate(object data);
}

[Authorize]
public class TmsHub : Hub<ITmsClient>
{
    /// <summary>
    /// Join the user's private notification group (keyed by ASP.NET Identity User ID).
    /// Called from Angular on hub connection start.
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
    }

    public async Task JoinStudentGroup(string studentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Student_{studentId}");
    }

    public async Task LeaveStudentGroup(string studentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Student_{studentId}");
    }

    public async Task JoinCourseGroup(string courseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Course_{courseId}");
    }

    public async Task LeaveCourseGroup(string courseId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Course_{courseId}");
    }

    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    }

    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
    }
}
