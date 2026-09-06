using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Commands;

public class UpdateInstructorCommandHandler(IInstructorService _instructorService) : IRequestHandler<UpdateInstructorCommand, bool>
{
    public async Task<bool> Handle(UpdateInstructorCommand request, CancellationToken cancellationToken)
    {
        return await _instructorService.UpdateInstructorAsync(request.Id, request.Dto, cancellationToken);
    }
}
