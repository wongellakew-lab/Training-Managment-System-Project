using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record AddModuleCommand(int CourseId, CreateModuleDto Dto) : IRequest<CourseModuleDto?>;
