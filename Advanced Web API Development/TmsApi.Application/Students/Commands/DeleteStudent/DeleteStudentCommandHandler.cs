using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Commands;

public class DeleteStudentCommandHandler(IStudentService _studentService) : IRequestHandler<DeleteStudentCommand, bool>
{
    public async Task<bool> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.DeleteStudentAsync(request.Id, cancellationToken);
    }
}
