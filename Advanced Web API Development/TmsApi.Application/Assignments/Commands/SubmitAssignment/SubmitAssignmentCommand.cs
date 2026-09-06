using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Assignments.Commands;

public record SubmitAssignmentCommand(int AssignmentId, int StudentId, SubmitAssignmentDto Dto) : IRequest<object?>;
