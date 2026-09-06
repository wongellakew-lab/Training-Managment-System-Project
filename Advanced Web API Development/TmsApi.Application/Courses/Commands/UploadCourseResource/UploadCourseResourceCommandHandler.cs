using MediatR;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class UploadCourseResourceCommandHandler(ICourseService _courseService)
    : IRequestHandler<UploadCourseResourceCommand, CourseResourceDto?>
{
    public async Task<CourseResourceDto?> Handle(UploadCourseResourceCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.UploadResourceAsync(request.CourseId, request.Title, request.File, cancellationToken);
    }
}
