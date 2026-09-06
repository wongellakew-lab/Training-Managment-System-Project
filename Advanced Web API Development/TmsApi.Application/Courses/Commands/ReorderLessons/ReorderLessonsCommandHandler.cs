using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class ReorderLessonsCommandHandler(ICourseService _courseService)
    : IRequestHandler<ReorderLessonsCommand, bool>
{
    public async Task<bool> Handle(ReorderLessonsCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.ReorderLessonsAsync(request.ModuleId, request.Items, cancellationToken);
    }
}
