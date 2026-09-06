using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Queries;

public sealed class GetAssignmentByIdQueryHandler(IAssignmentService _assignmentService)
    : IRequestHandler<GetAssignmentByIdQuery, AssignmentDto?>
{
    public async Task<AssignmentDto?> Handle(GetAssignmentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _assignmentService.GetAssignmentByIdAsync(request.Id, cancellationToken);
    }
}
