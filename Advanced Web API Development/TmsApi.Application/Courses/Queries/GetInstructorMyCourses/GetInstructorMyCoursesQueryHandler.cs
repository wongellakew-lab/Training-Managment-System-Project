using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Queries;

public sealed class GetInstructorMyCoursesQueryHandler(ICourseService _courseService)
    : IRequestHandler<GetInstructorMyCoursesQuery, List<InstructorCourseDto>>
{
    public async Task<List<InstructorCourseDto>> Handle(GetInstructorMyCoursesQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetInstructorMyCoursesAsync(request.InstructorId, cancellationToken);
    }
}
