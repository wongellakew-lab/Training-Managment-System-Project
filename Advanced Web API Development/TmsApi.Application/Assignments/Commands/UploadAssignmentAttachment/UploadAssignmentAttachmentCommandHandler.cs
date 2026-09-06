using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Commands;

public sealed class UploadAssignmentAttachmentCommandHandler(IAssignmentService _assignmentService)
    : IRequestHandler<UploadAssignmentAttachmentCommand, (string? url, string? fileName)>
{
    public async Task<(string? url, string? fileName)> Handle(UploadAssignmentAttachmentCommand request, CancellationToken cancellationToken)
    {
        return await _assignmentService.UploadAssignmentAttachmentAsync(request.Id, request.File, cancellationToken);
    }
}
