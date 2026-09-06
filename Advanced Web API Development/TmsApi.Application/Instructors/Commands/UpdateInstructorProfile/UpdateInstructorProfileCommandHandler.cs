using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Commands;

public class UpdateInstructorProfileCommandHandler(IInstructorService _instructorService) : IRequestHandler<UpdateInstructorProfileCommand, bool>
{
    public async Task<bool> Handle(UpdateInstructorProfileCommand request, CancellationToken cancellationToken)
    {
        return await _instructorService.UpdateProfileAsync(request.UserId, request.Dto, cancellationToken);
    }
}
