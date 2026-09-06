using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Commands;

public sealed class SubmitAssignmentCommandHandler(IAssignmentService _assignmentService)
    : IRequestHandler<SubmitAssignmentCommand, object?>
{
    public async Task<object?> Handle(SubmitAssignmentCommand request, CancellationToken cancellationToken)
    {
        return await _assignmentService.SubmitAssignmentAsync(request.AssignmentId, request.StudentId, request.Dto, cancellationToken);
    }
}
