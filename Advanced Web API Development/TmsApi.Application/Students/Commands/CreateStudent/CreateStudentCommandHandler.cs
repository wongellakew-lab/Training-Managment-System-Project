using MediatR;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Commands;

public class CreateStudentCommandHandler(IStudentService _studentService) : IRequestHandler<CreateStudentCommand, StudentDto>
{
    public async Task<StudentDto> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.CreateStudentAsync(request.Dto, cancellationToken);
    }
}
