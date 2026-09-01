using TmsApi.Application.DTOs.DashboardDtos;

namespace TmsApi.Application.Interfaces;

public interface IDashboardService
{
    Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId, CancellationToken cancellationToken = default);
    Task<InstructorDashboardDto> GetInstructorDashboardAsync(int instructorId, CancellationToken cancellationToken = default);
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}
