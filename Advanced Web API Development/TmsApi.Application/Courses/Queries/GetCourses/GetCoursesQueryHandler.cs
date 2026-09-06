using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Queries;

public sealed class GetCoursesQueryHandler(ICourseService _courseService)
    : IRequestHandler<GetCoursesQuery, PagedResponse<CourseDto>>
{
    public async Task<PagedResponse<CourseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetCoursesAsync(request.Request, cancellationToken);
    }
}
