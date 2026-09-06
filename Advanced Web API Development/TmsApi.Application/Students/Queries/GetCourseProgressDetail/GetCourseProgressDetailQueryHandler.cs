using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Queries;

public class GetCourseProgressDetailQueryHandler(IStudentService _studentService) : IRequestHandler<GetCourseProgressDetailQuery, object?>
{
    public async Task<object?> Handle(GetCourseProgressDetailQuery request, CancellationToken cancellationToken)
    {
        return await _studentService.GetCourseProgressDetailAsync(request.StudentId, request.CourseId, cancellationToken);
    }
}
