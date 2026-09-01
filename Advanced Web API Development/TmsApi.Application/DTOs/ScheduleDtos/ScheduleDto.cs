namespace TmsApi.Application.DTOs.ScheduleDtos;

public class ScheduleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Meetings"; // Meetings, Workshops, Events
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = "09:00 AM";
    public string EndTime { get; set; } = "10:00 AM";
    public string PicName { get; set; } = string.Empty;
    public string PicAvatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int AttendeesCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
}

public class CreateScheduleDto
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Meetings";
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = "09:00 AM";
    public string EndTime { get; set; } = "10:00 AM";
    public string PicName { get; set; } = string.Empty;
    public string PicAvatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int AttendeesCount { get; set; } = 0;
}

public class UpdateScheduleDto
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Meetings";
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = "09:00 AM";
    public string EndTime { get; set; } = "10:00 AM";
    public string PicName { get; set; } = string.Empty;
    public string PicAvatar { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int AttendeesCount { get; set; } = 0;
}
