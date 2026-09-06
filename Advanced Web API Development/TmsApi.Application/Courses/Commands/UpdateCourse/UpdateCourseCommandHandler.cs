using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class UpdateCourseCommandHandler(ICourseService _courseService)
    : IRequestHandler<UpdateCourseCommand, bool>
{
    public async Task<bool> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.UpdateCourseAsync(request.Id, request.Dto, cancellationToken);
    }
}
