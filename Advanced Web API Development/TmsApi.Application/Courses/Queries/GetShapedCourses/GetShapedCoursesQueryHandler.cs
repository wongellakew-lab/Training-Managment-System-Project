using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Queries;

public sealed class GetShapedCoursesQueryHandler(ICourseService _courseService)
    : IRequestHandler<GetShapedCoursesQuery, PagedResponse<System.Dynamic.ExpandoObject>>
{
    public async Task<PagedResponse<System.Dynamic.ExpandoObject>> Handle(GetShapedCoursesQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetShapedCoursesAsync(request.Request, cancellationToken);
    }
}
