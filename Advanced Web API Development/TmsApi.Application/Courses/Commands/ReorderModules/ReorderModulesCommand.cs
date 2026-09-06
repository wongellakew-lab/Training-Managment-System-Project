using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record ReorderModulesCommand(int CourseId, List<ReorderItemDto> Items) : IRequest<bool>;
