using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class DeleteLessonCommandHandler(ICourseService _courseService)
    : IRequestHandler<DeleteLessonCommand, bool>
{
    public async Task<bool> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.DeleteLessonAsync(request.LessonId, cancellationToken);
    }
}
