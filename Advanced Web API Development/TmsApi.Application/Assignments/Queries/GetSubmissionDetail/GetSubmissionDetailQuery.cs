using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Assignments.Queries;

public record GetSubmissionDetailQuery(int SubmissionId) : IRequest<AssignmentSubmissionDto?>;
