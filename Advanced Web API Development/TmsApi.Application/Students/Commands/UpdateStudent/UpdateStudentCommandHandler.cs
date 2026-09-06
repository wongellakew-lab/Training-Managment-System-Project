using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Commands;

public class UpdateStudentCommandHandler(IStudentService _studentService) : IRequestHandler<UpdateStudentCommand, bool>
{
    public async Task<bool> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.UpdateStudentAsync(request.Id, request.Dto, cancellationToken);
    }
}
