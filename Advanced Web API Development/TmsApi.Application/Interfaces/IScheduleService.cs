using TmsApi.Application.DTOs.ScheduleDtos;

namespace TmsApi.Application.Interfaces;

public interface IScheduleService
{
    Task<IEnumerable<ScheduleDto>> GetSchedulesAsync(string? category = null, int? month = null, int? year = null, int? studentId = null, int? instructorId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScheduleDto>> GetStudentSchedulesAsync(int studentId, string? view = null, DateTime? date = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScheduleDto>> GetInstructorSchedulesAsync(int instructorId, int? month = null, int? year = null, DateTime? date = null, CancellationToken cancellationToken = default);
    Task<StudentScheduleSummaryDto> GetStudentScheduleOverviewAsync(int studentId, int? year = null, int? month = null, int? courseId = null, CancellationToken cancellationToken = default);
    Task<ScheduleDto?> GetScheduleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ScheduleDto> CreateScheduleAsync(CreateScheduleDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateScheduleAsync(int id, UpdateScheduleDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteScheduleAsync(int id, CancellationToken cancellationToken = default);
}
