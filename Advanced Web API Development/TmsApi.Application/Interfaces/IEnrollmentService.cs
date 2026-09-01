using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface IEnrollmentService
{
    Task<PagedResponse<EnrollmentDto>> GetEnrollmentsAsync(int page, int pageSize, int? studentId = null, int? courseId = null, CancellationToken cancellationToken = default);
    Task<EnrollmentDto?> GetEnrollmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EnrollmentDto> CreateEnrollmentAsync(CreateEnrollmentDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateEnrollmentStatusAsync(int id, EnrollmentStatus status, CancellationToken cancellationToken = default);
}
