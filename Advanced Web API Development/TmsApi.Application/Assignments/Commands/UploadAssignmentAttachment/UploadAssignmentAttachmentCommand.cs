using MediatR;
using Microsoft.AspNetCore.Http;

namespace TmsApi.Application.Assignments.Commands;

public record UploadAssignmentAttachmentCommand(int Id, IFormFile File) : IRequest<(string? url, string? fileName)>;
