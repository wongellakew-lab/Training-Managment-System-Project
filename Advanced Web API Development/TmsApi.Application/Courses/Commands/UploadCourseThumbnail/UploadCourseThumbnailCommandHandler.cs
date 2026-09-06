using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class UploadCourseThumbnailCommandHandler(ICourseService _courseService)
    : IRequestHandler<UploadCourseThumbnailCommand, string?>
{
    public async Task<string?> Handle(UploadCourseThumbnailCommand request, CancellationToken cancellationToken)
    {
        return await _courseService.UploadThumbnailAsync(request.CourseId, request.File, cancellationToken);
    }
}
