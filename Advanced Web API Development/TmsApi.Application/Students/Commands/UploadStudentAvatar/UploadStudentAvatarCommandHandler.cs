using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Commands;

public class UploadStudentAvatarCommandHandler(IStudentService _studentService) : IRequestHandler<UploadStudentAvatarCommand, string?>
{
    public async Task<string?> Handle(UploadStudentAvatarCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.UploadAvatarAsync(request.UserId, request.File, cancellationToken);
    }
}
