using MediatR;

namespace TmsApi.Application.Assignments.Queries;

public record GetStudentAssignmentDetailQuery(int AssignmentId, int StudentId) : IRequest<object?>;
