using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record UpdateModuleCommand(int ModuleId, UpdateModuleDto Dto) : IRequest<bool>;
