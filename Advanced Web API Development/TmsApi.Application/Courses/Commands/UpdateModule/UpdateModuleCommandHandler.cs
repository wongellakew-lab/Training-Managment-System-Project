using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class UpdateModuleCommandHandler(ICourseService _courseService)
    : IRequestHandler<UpdateModuleCommand, bool>
{
    public async Task<bool> Handle(UpdateModuleCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.UpdateModuleAsync(request.ModuleId, request.Dto, cancellationToken);
    }
}
