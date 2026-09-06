using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Queries;

public sealed class GetCourseAssessmentConfigQueryHandler(IAssignmentService _assignmentService)
    : IRequestHandler<GetCourseAssessmentConfigQuery, object?>
{
    public async Task<object?> Handle(GetCourseAssessmentConfigQuery request, CancellationToken cancellationToken)
    {
        return await _assignmentService.GetCourseAssessmentConfigAsync(request.CourseId, cancellationToken);
    }
}
