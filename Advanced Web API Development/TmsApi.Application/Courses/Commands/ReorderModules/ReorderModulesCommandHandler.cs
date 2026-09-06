using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class ReorderModulesCommandHandler(ICourseService _courseService)
    : IRequestHandler<ReorderModulesCommand, bool>
{
    public async Task<bool> Handle(ReorderModulesCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.ReorderModulesAsync(request.CourseId, request.Items, cancellationToken);
    }
}
