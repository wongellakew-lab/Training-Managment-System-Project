using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Queries;

public sealed class GetMyAssignmentsQueryHandler(IAssignmentService _assignmentService)
    : IRequestHandler<GetMyAssignmentsQuery, List<object>>
{
    public async Task<List<object>> Handle(GetMyAssignmentsQuery request, CancellationToken cancellationToken)
    {
        return await _assignmentService.GetMyAssignmentsAsync(request.StudentId, cancellationToken);
    }
}
