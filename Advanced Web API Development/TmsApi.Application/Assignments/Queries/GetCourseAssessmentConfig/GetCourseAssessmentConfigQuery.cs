using MediatR;

namespace TmsApi.Application.Assignments.Queries;

public record GetCourseAssessmentConfigQuery(int CourseId) : IRequest<object?>;
