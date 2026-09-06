using MediatR;

namespace TmsApi.Application.Students.Queries;

public record GetStudentProgressSummaryQuery(int StudentId) : IRequest<object?>;
