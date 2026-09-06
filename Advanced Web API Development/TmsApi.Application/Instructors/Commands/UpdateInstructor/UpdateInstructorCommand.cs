using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Instructors.Commands;

public record UpdateInstructorCommand(int Id, UpdateInstructorDto Dto) : IRequest<bool>;
