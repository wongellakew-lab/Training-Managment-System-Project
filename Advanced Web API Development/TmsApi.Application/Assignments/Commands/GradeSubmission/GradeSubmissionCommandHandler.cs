using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Commands;

public sealed class GradeSubmissionCommandHandler(IAssignmentService _assignmentService)
    : IRequestHandler<GradeSubmissionCommand, bool>
{
    public async Task<bool> Handle(GradeSubmissionCommand request, CancellationToken cancellationToken)
    {
        return await _assignmentService.GradeSubmissionAsync(request.SubmissionId, request.Dto, cancellationToken);
    }
}
