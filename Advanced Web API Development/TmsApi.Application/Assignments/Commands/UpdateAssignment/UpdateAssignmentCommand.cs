using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Assignments.Commands;

public record UpdateAssignmentCommand(int Id, UpdateAssignmentDto Dto) : IRequest<bool>;
