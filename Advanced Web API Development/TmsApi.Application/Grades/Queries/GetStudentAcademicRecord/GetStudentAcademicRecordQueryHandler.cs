using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Grades.Queries;

public sealed class GetStudentAcademicRecordQueryHandler(IGradeService _gradeService)
    : IRequestHandler<GetStudentAcademicRecordQuery, object?>
{
    public async Task<object?> Handle(GetStudentAcademicRecordQuery request, CancellationToken cancellationToken)
    {
        return await _gradeService.GetStudentAcademicRecordAsync(request.StudentId, cancellationToken);
    }
}
