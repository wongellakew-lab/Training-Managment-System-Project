using MediatR;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Queries;

public class GetStudentProfileQueryHandler(IStudentService _studentService) : IRequestHandler<GetStudentProfileQuery, StudentProfileDto?>
{
    public async Task<StudentProfileDto?> Handle(GetStudentProfileQuery request, CancellationToken cancellationToken)
    {
        return await _studentService.GetProfileAsync(request.UserId, cancellationToken);
    }
}
