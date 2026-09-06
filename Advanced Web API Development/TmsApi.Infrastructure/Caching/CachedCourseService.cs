using System.Dynamic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Caching;

public class CachedCourseService(
    ICourseService _innerService,
    HybridCache _hybridCache) : ICachedCourseService
{
    private const string CacheTag = "courses";

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

    public async Task EnsureCourseAssessmentsGeneratedAsync(int courseId, CancellationToken cancellationToken = default)
    {
        await _innerService.EnsureCourseAssessmentsGeneratedAsync(courseId, cancellationToken);
    }

    public async Task<bool> ApproveCourseAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.ApproveCourseAsync(id, cancellationToken);
        if (result) await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> RejectCourseAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.RejectCourseAsync(id, reason, cancellationToken);
        if (result) await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<CourseBuilderDto?> GetCourseForBuilderAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _innerService.GetCourseForBuilderAsync(id, cancellationToken);
    }

    public async Task<CourseModuleDto?> AddModuleAsync(int courseId, CreateModuleDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.AddModuleAsync(courseId, dto, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> UpdateModuleAsync(int moduleId, UpdateModuleDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.UpdateModuleAsync(moduleId, dto, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> DeleteModuleAsync(int moduleId, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.DeleteModuleAsync(moduleId, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> ReorderModulesAsync(int courseId, List<ReorderItemDto> items, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.ReorderModulesAsync(courseId, items, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<CourseLessonDto?> AddLessonAsync(int moduleId, CreateLessonDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.AddLessonAsync(moduleId, dto, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> UpdateLessonAsync(int lessonId, UpdateLessonDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.UpdateLessonAsync(lessonId, dto, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> DeleteLessonAsync(int lessonId, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.DeleteLessonAsync(lessonId, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> ReorderLessonsAsync(int moduleId, List<ReorderItemDto> items, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.ReorderLessonsAsync(moduleId, items, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<string?> UploadThumbnailAsync(int courseId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.UploadThumbnailAsync(courseId, file, cancellationToken);
        if (result != null) await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<CourseResourceDto?> UploadResourceAsync(int courseId, string title, IFormFile file, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.UploadResourceAsync(courseId, title, file, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<bool> DeleteResourceAsync(int resourceId, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.DeleteResourceAsync(resourceId, cancellationToken);
        await _hybridCache.RemoveByTagAsync(CacheTag, cancellationToken);
        return result;
    }

    public async Task<List<InstructorCourseDto>> GetInstructorMyCoursesAsync(int instructorId, CancellationToken cancellationToken = default)
    {
        return await _innerService.GetInstructorMyCoursesAsync(instructorId, cancellationToken);
    }
}
