using MediatR;

namespace TmsApi.Application.Students.Queries;

public record GetCourseProgressDetailQuery(int StudentId, int CourseId) : IRequest<object?>;
