using MediatR;

namespace TmsApi.Application.Courses.Commands;

public record DeleteModuleCommand(int ModuleId) : IRequest<bool>;
