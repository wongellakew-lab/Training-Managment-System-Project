using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Commands;

public class DeleteInstructorCommandHandler(IInstructorService _instructorService) : IRequestHandler<DeleteInstructorCommand, bool>
{
    public async Task<bool> Handle(DeleteInstructorCommand request, CancellationToken cancellationToken)
    {
        return await _instructorService.DeleteInstructorAsync(request.Id, cancellationToken);
    }
}
