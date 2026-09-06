using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Assignments.Commands;

public record GradeSubmissionCommand(int SubmissionId, GradeSubmissionDto Dto) : IRequest<bool>;
