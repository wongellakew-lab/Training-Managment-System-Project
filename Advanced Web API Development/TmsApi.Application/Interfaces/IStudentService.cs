using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;

namespace TmsApi.Application.Interfaces;

public interface IStudentService
{
    Task<PagedResponse<StudentDto>> GetStudentsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<StudentDto?> GetStudentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StudentDto> CreateStudentAsync(CreateStudentDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto, CancellationToken cancellationToken = default);
}
