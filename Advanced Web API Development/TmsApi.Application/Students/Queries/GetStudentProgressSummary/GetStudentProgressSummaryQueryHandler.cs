using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Queries;

public class GetStudentProgressSummaryQueryHandler(IStudentService _studentService) : IRequestHandler<GetStudentProgressSummaryQuery, object?>
{
    public async Task<object?> Handle(GetStudentProgressSummaryQuery request, CancellationToken cancellationToken)
    {
        return await _studentService.GetProgressSummaryAsync(request.StudentId, cancellationToken);
    }
}
