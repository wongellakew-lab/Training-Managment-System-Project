using MediatR;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Queries;

public class GetStudentStatsQueryHandler(IStudentService _studentService) : IRequestHandler<GetStudentStatsQuery, StudentStatsDto>
{
    public async Task<StudentStatsDto> Handle(GetStudentStatsQuery request, CancellationToken cancellationToken)
    {
        return await _studentService.GetStudentStatsAsync(cancellationToken);
    }
}
