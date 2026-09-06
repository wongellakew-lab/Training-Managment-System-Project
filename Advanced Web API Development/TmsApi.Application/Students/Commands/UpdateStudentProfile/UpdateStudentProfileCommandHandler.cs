using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Commands;

public class UpdateStudentProfileCommandHandler(IStudentService _studentService) : IRequestHandler<UpdateStudentProfileCommand, bool>
{
    public async Task<bool> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.UpdateProfileAsync(request.UserId, request.Dto, cancellationToken);
    }
}
