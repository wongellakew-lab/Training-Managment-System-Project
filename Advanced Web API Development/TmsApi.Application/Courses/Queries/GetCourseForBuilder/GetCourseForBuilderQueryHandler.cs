using MediatR;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Queries;

public sealed class GetCourseForBuilderQueryHandler(ICourseService _courseService)
    : IRequestHandler<GetCourseForBuilderQuery, CourseBuilderDto?>
{
    public async Task<CourseBuilderDto?> Handle(GetCourseForBuilderQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetCourseForBuilderAsync(request.Id, cancellationToken);
    }
}
