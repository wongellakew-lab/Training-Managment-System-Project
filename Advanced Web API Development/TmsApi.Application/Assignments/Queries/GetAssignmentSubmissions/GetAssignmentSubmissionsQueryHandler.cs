using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Queries;

public sealed class GetAssignmentSubmissionsQueryHandler(IAssignmentService _assignmentService)
    : IRequestHandler<GetAssignmentSubmissionsQuery, List<AssignmentSubmissionDto>>
{
    public async Task<List<AssignmentSubmissionDto>> Handle(GetAssignmentSubmissionsQuery request, CancellationToken cancellationToken)
    {
        return await _assignmentService.GetAssignmentSubmissionsAsync(request.AssignmentId, cancellationToken);
    }
}
