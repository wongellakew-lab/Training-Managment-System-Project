using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Queries;

public record GetCoursesQuery(PagedRequest Request) : IRequest<PagedResponse<CourseDto>>;

public class GetCoursesHandler : IRequestHandler<GetCoursesQuery, PagedResponse<CourseDto>>
{
    private readonly ICourseService _courseService;

    public GetCoursesHandler(ICourseService courseService)
    {
        _courseService = courseService;
    }

    public async Task<PagedResponse<CourseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetCoursesAsync(request.Request, cancellationToken);
    }
}
