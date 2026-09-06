using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Commands;

public class UploadInstructorAvatarCommandHandler(IInstructorService _instructorService) : IRequestHandler<UploadInstructorAvatarCommand, string?>
{
    public async Task<string?> Handle(UploadInstructorAvatarCommand request, CancellationToken cancellationToken)
    {
        return await _instructorService.UploadAvatarAsync(request.UserId, request.File, cancellationToken);
    }
}
