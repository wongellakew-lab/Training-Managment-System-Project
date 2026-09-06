using MediatR;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Queries;

public sealed class GetCourseByIdQueryHandler(ICourseService _courseService)
    : IRequestHandler<GetCourseByIdQuery, CourseDto?>
{
    public async Task<CourseDto?> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetCourseByIdAsync(request.Id, cancellationToken);
    }
}
