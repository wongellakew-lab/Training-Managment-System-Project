using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class UpdateLessonCommandHandler(ICourseService _courseService)
    : IRequestHandler<UpdateLessonCommand, bool>
{
    public async Task<bool> Handle(UpdateLessonCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.UpdateLessonAsync(request.LessonId, request.Dto, cancellationToken);
    }
}
