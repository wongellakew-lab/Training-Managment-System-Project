using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Grades.Commands;

public sealed class UpdateGradebookCellCommandHandler(IGradeService _gradeService)
    : IRequestHandler<UpdateGradebookCellCommand, object?>
{
    public async Task<object?> Handle(UpdateGradebookCellCommand request, CancellationToken cancellationToken)
    {
        return await _gradeService.UpdateGradebookCellAsync(request.Dto, cancellationToken);
    }
}
