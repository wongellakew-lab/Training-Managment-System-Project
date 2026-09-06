using MediatR;

namespace TmsApi.Application.Courses.Commands;

public record EnsureAssessmentsCommand(int CourseId) : IRequest<Unit>;
