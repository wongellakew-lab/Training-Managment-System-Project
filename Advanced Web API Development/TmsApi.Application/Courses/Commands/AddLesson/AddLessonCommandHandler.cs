using MediatR;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class AddLessonCommandHandler(ICourseService _courseService)
    : IRequestHandler<AddLessonCommand, CourseLessonDto?>
{
    public async Task<CourseLessonDto?> Handle(AddLessonCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.AddLessonAsync(request.ModuleId, request.Dto, cancellationToken);
    }
}
