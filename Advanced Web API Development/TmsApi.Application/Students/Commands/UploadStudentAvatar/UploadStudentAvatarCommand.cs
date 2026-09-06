using MediatR;
using Microsoft.AspNetCore.Http;

namespace TmsApi.Application.Students.Commands;

public record UploadStudentAvatarCommand(string UserId, IFormFile File) : IRequest<string?>;
