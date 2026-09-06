using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record ReorderLessonsCommand(int ModuleId, List<ReorderItemDto> Items) : IRequest<bool>;
