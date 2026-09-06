using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Grades.Commands;

public sealed class BulkAssignGradesCommandHandler(IGradeService _gradeService)
    : IRequestHandler<BulkAssignGradesCommand, List<object>>
{
    public async Task<List<object>> Handle(BulkAssignGradesCommand request, CancellationToken cancellationToken)
    {
        return await _gradeService.BulkAssignGradesAsync(request.Dto, cancellationToken);
    }
}
