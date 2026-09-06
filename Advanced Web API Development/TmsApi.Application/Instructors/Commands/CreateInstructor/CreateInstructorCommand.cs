using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Instructors.Commands;

public record CreateInstructorCommand(CreateInstructorDto Dto) : IRequest<InstructorDto>;
