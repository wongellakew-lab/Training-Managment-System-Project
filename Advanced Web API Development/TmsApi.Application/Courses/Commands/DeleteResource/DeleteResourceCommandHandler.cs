using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class DeleteResourceCommandHandler(ICourseService _courseService)
    : IRequestHandler<DeleteResourceCommand, bool>
{
    public async Task<bool> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.DeleteResourceAsync(request.ResourceId, cancellationToken);
    }
}
