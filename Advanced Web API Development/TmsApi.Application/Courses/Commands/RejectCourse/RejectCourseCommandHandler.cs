using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class RejectCourseCommandHandler(ICourseService _courseService)
    : IRequestHandler<RejectCourseCommand, bool>
{
    public async Task<bool> Handle(RejectCourseCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.RejectCourseAsync(request.Id, request.Reason, cancellationToken);
    }
}
