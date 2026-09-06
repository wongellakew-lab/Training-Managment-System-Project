using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Instructors.Commands;

public record SuspendInstructorCommand(int Id, SuspendInstructorRequestDto Dto) : IRequest<bool>;
