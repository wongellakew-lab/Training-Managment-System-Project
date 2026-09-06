using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Queries;

public sealed class GetAssignmentsByCourseQueryHandler(IAssignmentService _assignmentService)
    : IRequestHandler<GetAssignmentsByCourseQuery, List<AssignmentDto>>
{
    public async Task<List<AssignmentDto>> Handle(GetAssignmentsByCourseQuery request, CancellationToken cancellationToken)
    {
        return await _assignmentService.GetAssignmentsByCourseAsync(request.CourseId, cancellationToken);
    }
}
