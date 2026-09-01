using Microsoft.AspNetCore.SignalR;

namespace TmsApi.Infrastructure.Hubs;

public interface ITmsClient
{
    Task ReceiveEnrollmentUpdate(string message, object data);
    Task ReceiveNotification(string title, string message);
    Task ReceiveTranscriptStatus(string requestId, string status, string? downloadUrl);
}

public class TmsHub : Hub<ITmsClient>
{
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
}
