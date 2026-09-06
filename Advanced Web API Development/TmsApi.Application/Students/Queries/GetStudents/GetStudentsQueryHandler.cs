using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Queries;

public class GetStudentsQueryHandler(IStudentService _studentService) : IRequestHandler<GetStudentsQuery, PagedResponse<StudentDto>>
{
    public async Task<PagedResponse<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        return await _studentService.GetStudentsAsync(request.Request, cancellationToken);
    }
}
