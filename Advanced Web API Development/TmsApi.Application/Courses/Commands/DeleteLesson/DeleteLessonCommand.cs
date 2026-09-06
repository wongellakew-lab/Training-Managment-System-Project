using MediatR;

namespace TmsApi.Application.Courses.Commands;

public record DeleteLessonCommand(int LessonId) : IRequest<bool>;
