using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Assignments.Queries;

public record GetAssignmentByIdQuery(int Id) : IRequest<AssignmentDto?>;
