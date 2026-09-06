using MediatR;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class AddModuleCommandHandler(ICourseService _courseService)
    : IRequestHandler<AddModuleCommand, CourseModuleDto?>
{
    public async Task<CourseModuleDto?> Handle(AddModuleCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.AddModuleAsync(request.CourseId, request.Dto, cancellationToken);
    }
}
