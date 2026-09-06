using Microsoft.AspNetCore.Http;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Interfaces;

public interface IInstructorService
{
    Task<PagedResponse<InstructorDto>> GetInstructorsAsync(PagedRequest request, string? department, CancellationToken cancellationToken = default);
    Task<InstructorDetailDto?> GetInstructorByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InstructorDto> CreateInstructorAsync(CreateInstructorDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateInstructorAsync(int id, UpdateInstructorDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteInstructorAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SuspendInstructorAsync(int id, SuspendInstructorRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> ActivateInstructorAsync(int id, CancellationToken cancellationToken = default);
    Task<InstructorProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateProfileAsync(string userId, UpdateInstructorProfileDto dto, CancellationToken cancellationToken = default);
    Task<string?> UploadAvatarAsync(string userId, IFormFile file, CancellationToken cancellationToken = default);
    Task<InstructorStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
