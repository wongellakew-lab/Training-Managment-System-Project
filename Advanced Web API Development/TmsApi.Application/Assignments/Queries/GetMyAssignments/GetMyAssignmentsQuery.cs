using MediatR;

namespace TmsApi.Application.Assignments.Queries;

public record GetMyAssignmentsQuery(int StudentId) : IRequest<List<object>>;
