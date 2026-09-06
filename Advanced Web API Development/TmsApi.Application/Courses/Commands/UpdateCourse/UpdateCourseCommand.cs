using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record UpdateCourseCommand(int Id, UpdateCourseDto Dto) : IRequest<bool>;
