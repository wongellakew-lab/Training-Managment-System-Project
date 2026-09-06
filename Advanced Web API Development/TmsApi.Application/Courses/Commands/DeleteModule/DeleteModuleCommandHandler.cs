using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class DeleteModuleCommandHandler(ICourseService _courseService)
    : IRequestHandler<DeleteModuleCommand, bool>
{
    public async Task<bool> Handle(DeleteModuleCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.DeleteModuleAsync(request.ModuleId, cancellationToken);
    }
}
