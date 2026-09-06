using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Commands;

public sealed class CreateAssignmentCommandHandler(IAssignmentService _assignmentService)
    : IRequestHandler<CreateAssignmentCommand, int>
{
    public async Task<int> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
    {
        return await _assignmentService.CreateAssignmentAsync(request.Dto, request.InstructorId, cancellationToken);
    }
}
