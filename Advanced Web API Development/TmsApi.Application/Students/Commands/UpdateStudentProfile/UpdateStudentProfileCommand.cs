using MediatR;
using TmsApi.Application.DTOs.StudentDtos;

namespace TmsApi.Application.Students.Commands;

public record UpdateStudentProfileCommand(string UserId, UpdateStudentProfileDto Dto) : IRequest<bool>;
