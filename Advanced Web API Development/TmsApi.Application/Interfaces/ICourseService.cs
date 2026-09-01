using System.Dynamic;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Interfaces;

public interface ICourseService
{
    Task<PagedResponse<CourseDto>> GetCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ExpandoObject>> GetShapedCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<CourseDto?> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken = default);
}

public interface ICachedCourseService : ICourseService
{
}
