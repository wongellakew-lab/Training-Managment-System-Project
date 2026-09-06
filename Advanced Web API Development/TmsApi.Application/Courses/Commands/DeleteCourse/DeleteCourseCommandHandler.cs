using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class DeleteCourseCommandHandler(ICourseService _courseService)
    : IRequestHandler<DeleteCourseCommand, bool>
{
    public async Task<bool> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.DeleteCourseAsync(request.Id, cancellationToken);
    }
}
