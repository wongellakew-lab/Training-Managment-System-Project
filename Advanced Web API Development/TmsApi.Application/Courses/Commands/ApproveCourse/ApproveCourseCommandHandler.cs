using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class ApproveCourseCommandHandler(ICourseService _courseService)
    : IRequestHandler<ApproveCourseCommand, bool>
{
    public async Task<bool> Handle(ApproveCourseCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.ApproveCourseAsync(request.Id, cancellationToken);
    }
}
