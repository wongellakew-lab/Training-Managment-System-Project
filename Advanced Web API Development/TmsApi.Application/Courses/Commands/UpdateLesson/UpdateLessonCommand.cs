using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record UpdateLessonCommand(int LessonId, UpdateLessonDto Dto) : IRequest<bool>;
