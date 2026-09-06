using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Commands;

public class SuspendInstructorCommandHandler(IInstructorService _instructorService) : IRequestHandler<SuspendInstructorCommand, bool>
{
    public async Task<bool> Handle(SuspendInstructorCommand request, CancellationToken cancellationToken)
    {
        return await _instructorService.SuspendInstructorAsync(request.Id, request.Dto, cancellationToken);
    }
}
