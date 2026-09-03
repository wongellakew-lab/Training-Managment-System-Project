namespace TmsApi.Domain.Entities;

public class CourseSchedule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; } // Sunday = 0, Monday = 1, ..., Saturday = 6
    public string StartTime { get; set; } = "09:00 AM";
    public string EndTime { get; set; } = "10:30 AM";
    public string Location { get; set; } = "Main Training Center";
    public string Room { get; set; } = "Room 204";
    public string ScheduleType { get; set; } = "Lecture"; // Lecture, Lab, Workshop, Discussion
    public bool IsDeleted { get; set; } = false;
}
