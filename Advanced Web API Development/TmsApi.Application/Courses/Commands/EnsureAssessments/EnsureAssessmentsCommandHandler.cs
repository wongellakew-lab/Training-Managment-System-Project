using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public sealed class EnsureAssessmentsCommandHandler(ICourseService _courseService)
    : IRequestHandler<EnsureAssessmentsCommand, Unit>
{
    public async Task<Unit> Handle(EnsureAssessmentsCommand request, CancellationToken cancellationToken)
    {
        await _courseService.EnsureCourseAssessmentsGeneratedAsync(request.CourseId, cancellationToken);
        return Unit.Value;
    }
}
