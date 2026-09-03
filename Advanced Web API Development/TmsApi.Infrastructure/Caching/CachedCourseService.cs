using System.Dynamic;
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Caching;

public class CachedCourseService : ICachedCourseService
{
    private readonly ICourseService _innerService;
    private readonly HybridCache _hybridCache;
    private const string CacheTag = "courses";

    public CachedCourseService(ICourseService innerService, HybridCache hybridCache)
    {
        _innerService = innerService;
        _hybridCache = hybridCache;
    }

    public async Task<PagedResponse<CourseDto>> GetCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"courses_{request.Page}_{request.PageSize}_{request.SearchTerm}_{request.Department}_{request.Category}_{request.Status}_{request.SortBy}_{request.SortDescending}";
        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel => await _innerService.GetCoursesAsync(request, cancel),
            tags: [CacheTag],
            cancellationToken: cancellationToken);
    }

    public async Task<PagedResponse<ExpandoObject>> GetShapedCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        return await _innerService.GetShapedCoursesAsync(request, cancellationToken);
    }

    public async Task<CourseDto?> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"course_{id}";
        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel => await _innerService.GetCourseByIdAsync(id, cancel),
            tags: [CacheTag],
            cancellationToken: cancellationToken);
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.CreateCourseAsync(dto, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.UpdateCourseAsync(id, dto, cancellationToken);
        if (result)
        {
            await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        }
        return result;
    }

    public async Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.DeleteCourseAsync(id, cancellationToken);
        if (result)
        {
            await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        }
        return result;
    }

    public async Task<List<StudentEnrolledCourseDto>> GetStudentEnrolledCoursesAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _innerService.GetStudentEnrolledCoursesAsync(studentId, cancellationToken);
    }

    public async Task<StudentCoursesStatisticsDto> GetStudentCoursesStatisticsAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _innerService.GetStudentCoursesStatisticsAsync(studentId, cancellationToken);
    }
}
