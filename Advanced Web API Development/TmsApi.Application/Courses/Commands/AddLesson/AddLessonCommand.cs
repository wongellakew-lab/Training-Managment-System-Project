using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record AddLessonCommand(int ModuleId, CreateLessonDto Dto) : IRequest<CourseLessonDto?>;
