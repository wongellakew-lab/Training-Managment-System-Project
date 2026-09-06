using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Grades.Commands;

public sealed class SubmitBulkGradebookCommandHandler(IGradeService _gradeService)
    : IRequestHandler<SubmitBulkGradebookCommand, object?>
{
    public async Task<object?> Handle(SubmitBulkGradebookCommand request, CancellationToken cancellationToken)
    {
        return await _gradeService.SubmitBulkGradebookAsync(request.Dto, cancellationToken);
    }
}
