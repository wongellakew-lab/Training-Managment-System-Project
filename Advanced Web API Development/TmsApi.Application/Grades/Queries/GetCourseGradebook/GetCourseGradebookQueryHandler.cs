using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Grades.Queries;

public sealed class GetCourseGradebookQueryHandler(IGradeService _gradeService)
    : IRequestHandler<GetCourseGradebookQuery, object?>
{
    public async Task<object?> Handle(GetCourseGradebookQuery request, CancellationToken cancellationToken)
    {
        return await _gradeService.GetCourseGradebookAsync(request.CourseId, cancellationToken);
    }
}
