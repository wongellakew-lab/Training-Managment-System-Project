using MediatR;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Queries;

public class GetStudentDetailQueryHandler(IStudentService _studentService) : IRequestHandler<GetStudentDetailQuery, AdminStudentDetailDto?>
{
    public async Task<AdminStudentDetailDto?> Handle(GetStudentDetailQuery request, CancellationToken cancellationToken)
    {
        return await _studentService.GetStudentDetailAsync(request.Id, cancellationToken);
    }
}
