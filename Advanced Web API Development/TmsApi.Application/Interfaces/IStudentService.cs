using Microsoft.AspNetCore.Http;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;

namespace TmsApi.Application.Interfaces;

public interface IStudentService
{
    Task<PagedResponse<StudentDto>> GetStudentsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<StudentDto?> GetStudentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminStudentDetailDto?> GetStudentDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<StudentStatsDto> GetStudentStatsAsync(CancellationToken cancellationToken = default);
    Task<StudentDto> CreateStudentAsync(CreateStudentDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteStudentAsync(int id, CancellationToken cancellationToken = default);

    Task<StudentProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateProfileAsync(string userId, UpdateStudentProfileDto dto, CancellationToken cancellationToken = default);
    Task<string?> UploadAvatarAsync(string userId, IFormFile file, CancellationToken cancellationToken = default);

    Task<object?> GetProgressSummaryAsync(int studentId, CancellationToken cancellationToken = default);
    Task<object?> GetCourseProgressDetailAsync(int studentId, int courseId, CancellationToken cancellationToken = default);
    Task<bool> WithdrawFromCourseAsync(int studentId, int courseId, CancellationToken cancellationToken = default);
}
