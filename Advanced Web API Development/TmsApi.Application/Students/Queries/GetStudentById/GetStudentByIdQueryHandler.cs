using MediatR;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Queries;

public class GetStudentByIdQueryHandler(IStudentService _studentService) : IRequestHandler<GetStudentByIdQuery, StudentDto?>
{
    public async Task<StudentDto?> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _studentService.GetStudentByIdAsync(request.Id, cancellationToken);
    }
}
