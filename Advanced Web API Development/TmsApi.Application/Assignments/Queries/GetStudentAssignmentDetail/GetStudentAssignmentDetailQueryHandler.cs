using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Assignments.Queries;

public sealed class GetStudentAssignmentDetailQueryHandler(IAssignmentService _assignmentService)
    : IRequestHandler<GetStudentAssignmentDetailQuery, object?>
{
    public async Task<object?> Handle(GetStudentAssignmentDetailQuery request, CancellationToken cancellationToken)
    {
        return await _assignmentService.GetStudentAssignmentDetailAsync(request.AssignmentId, request.StudentId, cancellationToken);
    }
}
