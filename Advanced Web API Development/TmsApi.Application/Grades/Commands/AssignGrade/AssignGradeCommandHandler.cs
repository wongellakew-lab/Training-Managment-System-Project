using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Grades.Commands;

public sealed class AssignGradeCommandHandler(IGradeService _gradeService)
    : IRequestHandler<AssignGradeCommand, object?>
{
    public async Task<object?> Handle(AssignGradeCommand request, CancellationToken cancellationToken)
    {
        return await _gradeService.AssignGradeAsync(request.Dto, cancellationToken);
    }
}
