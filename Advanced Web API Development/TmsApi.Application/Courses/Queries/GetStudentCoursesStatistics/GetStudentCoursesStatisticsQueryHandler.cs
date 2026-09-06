using MediatR;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Queries;

public sealed class GetStudentCoursesStatisticsQueryHandler(ICourseService _courseService)
    : IRequestHandler<GetStudentCoursesStatisticsQuery, StudentCoursesStatisticsDto>
{
    public async Task<StudentCoursesStatisticsDto> Handle(GetStudentCoursesStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _courseService.GetStudentCoursesStatisticsAsync(request.StudentId, cancellationToken);
    }
}
