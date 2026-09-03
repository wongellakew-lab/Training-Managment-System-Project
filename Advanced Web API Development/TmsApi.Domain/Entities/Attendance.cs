namespace TmsApi.Domain.Entities;

public enum AttendanceStatus
{
    Present = 1,
    Late = 2,
    Absent = 3
}

public class Attendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }

    public DateTime SessionDate { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Remarks { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public int? RecordedByInstructorId { get; set; }
    public Instructor? RecordedByInstructor { get; set; }
}
