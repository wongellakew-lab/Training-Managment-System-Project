using MediatR;

namespace TmsApi.Application.Courses.Commands;

public record DeleteResourceCommand(int ResourceId) : IRequest<bool>;
