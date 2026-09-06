using MediatR;

namespace TmsApi.Application.Grades.Queries;

public record GetCourseGradebookQuery(int CourseId) : IRequest<object?>;
