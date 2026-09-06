using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Commands;

public class ActivateInstructorCommandHandler(IInstructorService _instructorService) : IRequestHandler<ActivateInstructorCommand, bool>
{
    public async Task<bool> Handle(ActivateInstructorCommand request, CancellationToken cancellationToken)
    {
        return await _instructorService.ActivateInstructorAsync(request.Id, cancellationToken);
    }
}
