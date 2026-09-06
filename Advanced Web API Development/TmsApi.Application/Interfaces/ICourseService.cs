using System.Dynamic;
using Microsoft.AspNetCore.Http;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Interfaces;

public interface ICourseService
{
    Task<PagedResponse<CourseDto>> GetCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ExpandoObject>> GetShapedCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<CourseDto?> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken = default);
    Task<List<StudentEnrolledCourseDto>> GetStudentEnrolledCoursesAsync(int studentId, CancellationToken cancellationToken = default);
    Task<StudentCoursesStatisticsDto> GetStudentCoursesStatisticsAsync(int studentId, CancellationToken cancellationToken = default);
    Task EnsureCourseAssessmentsGeneratedAsync(int courseId, CancellationToken cancellationToken = default);

    Task<bool> ApproveCourseAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> RejectCourseAsync(int id, string reason, CancellationToken cancellationToken = default);
    Task<CourseBuilderDto?> GetCourseForBuilderAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseModuleDto?> AddModuleAsync(int courseId, CreateModuleDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateModuleAsync(int moduleId, UpdateModuleDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteModuleAsync(int moduleId, CancellationToken cancellationToken = default);
    Task<bool> ReorderModulesAsync(int courseId, List<ReorderItemDto> items, CancellationToken cancellationToken = default);
    Task<CourseLessonDto?> AddLessonAsync(int moduleId, CreateLessonDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateLessonAsync(int lessonId, UpdateLessonDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteLessonAsync(int lessonId, CancellationToken cancellationToken = default);
    Task<bool> ReorderLessonsAsync(int moduleId, List<ReorderItemDto> items, CancellationToken cancellationToken = default);
    Task<string?> UploadThumbnailAsync(int courseId, IFormFile file, CancellationToken cancellationToken = default);
    Task<CourseResourceDto?> UploadResourceAsync(int courseId, string title, IFormFile file, CancellationToken cancellationToken = default);
    Task<bool> DeleteResourceAsync(int resourceId, CancellationToken cancellationToken = default);
    Task<List<InstructorCourseDto>> GetInstructorMyCoursesAsync(int instructorId, CancellationToken cancellationToken = default);
}

public interface ICachedCourseService : ICourseService
{
}
