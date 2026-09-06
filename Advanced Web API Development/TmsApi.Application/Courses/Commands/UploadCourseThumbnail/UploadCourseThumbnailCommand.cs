using MediatR;
using Microsoft.AspNetCore.Http;

namespace TmsApi.Application.Courses.Commands;

public record UploadCourseThumbnailCommand(int CourseId, IFormFile File) : IRequest<string?>;
