using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Commands;

public sealed class DeleteAssignmentCommandHandler(IAssignmentService _assignmentService)
    : IRequestHandler<DeleteAssignmentCommand, bool>
{
    public async Task<bool> Handle(DeleteAssignmentCommand request, CancellationToken cancellationToken)
    {
        return await _assignmentService.DeleteAssignmentAsync(request.Id, cancellationToken);
    }
}
