using MediatR;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Queries;

public sealed class GetStudentEnrolledCoursesQueryHandler(ICourseService _courseService)
    : IRequestHandler<GetStudentEnrolledCoursesQuery, List<StudentEnrolledCourseDto>>
{
    public async Task<List<StudentEnrolledCourseDto>> Handle(GetStudentEnrolledCoursesQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetStudentEnrolledCoursesAsync(request.StudentId, cancellationToken);
    }
}
