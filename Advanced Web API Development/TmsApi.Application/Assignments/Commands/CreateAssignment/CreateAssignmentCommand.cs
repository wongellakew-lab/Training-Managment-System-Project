using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Assignments.Commands;

public record CreateAssignmentCommand(CreateAssignmentDto Dto, int? InstructorId = null) : IRequest<int>;
