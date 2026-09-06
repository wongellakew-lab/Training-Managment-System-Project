using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Instructors.Commands;

public record UpdateInstructorProfileCommand(string UserId, UpdateInstructorProfileDto Dto) : IRequest<bool>;
