using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public class GetEnrollmentsQueryHandler(IEnrollmentService _enrollmentService)
    : IRequestHandler<GetEnrollmentsQuery, PagedResponse<EnrollmentDto>>
{
    public async Task<PagedResponse<EnrollmentDto>> Handle(GetEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        return await _enrollmentService.GetEnrollmentsAsync(
            request.Page,
            request.PageSize,
            request.StudentId,
            request.CourseId,
            request.Search,
            request.Status,
            request.Category,
            cancellationToken);
    }
}

// Backward compatibility alias
public class GetEnrollmentsHandler(IEnrollmentService _enrollmentService)
    : GetEnrollmentsQueryHandler(_enrollmentService)
{
}
