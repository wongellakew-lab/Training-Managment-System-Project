using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Queries;

public sealed class GetSubmissionDetailQueryHandler(IAssignmentService _assignmentService)
    : IRequestHandler<GetSubmissionDetailQuery, AssignmentSubmissionDto?>
{
    public async Task<AssignmentSubmissionDto?> Handle(GetSubmissionDetailQuery request, CancellationToken cancellationToken)
    {
        return await _assignmentService.GetSubmissionDetailAsync(request.SubmissionId, cancellationToken);
    }
}
