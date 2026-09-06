using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Commands;

public class UpdateEnrollmentStatusCommandHandler(IEnrollmentService _enrollmentService)
    : IRequestHandler<UpdateEnrollmentStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateEnrollmentStatusCommand request, CancellationToken cancellationToken)
    {
        return await _enrollmentService.UpdateEnrollmentStatusAsync(request.Id, request.Status, cancellationToken);
    }
}

// Backward compatibility alias
public class UpdateEnrollmentStatusHandler(IEnrollmentService _enrollmentService)
    : UpdateEnrollmentStatusCommandHandler(_enrollmentService)
{
}
