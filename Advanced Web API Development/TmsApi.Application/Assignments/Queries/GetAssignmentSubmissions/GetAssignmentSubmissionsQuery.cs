using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Assignments.Queries;

public record GetAssignmentSubmissionsQuery(int AssignmentId) : IRequest<List<AssignmentSubmissionDto>>;
