using MediatR;
using Microsoft.AspNetCore.Http;

namespace TmsApi.Application.Instructors.Commands;

public record UploadInstructorAvatarCommand(string UserId, IFormFile File) : IRequest<string?>;
