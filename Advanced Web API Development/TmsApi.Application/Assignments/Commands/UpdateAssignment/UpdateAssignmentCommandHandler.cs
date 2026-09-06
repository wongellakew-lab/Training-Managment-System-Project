using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Commands;

public sealed class UpdateAssignmentCommandHandler(IAssignmentService _assignmentService)
    : IRequestHandler<UpdateAssignmentCommand, bool>
{
    public async Task<bool> Handle(UpdateAssignmentCommand request, CancellationToken cancellationToken)
    {
        return await _assignmentService.UpdateAssignmentAsync(request.Id, request.Dto, cancellationToken);
    }
}
