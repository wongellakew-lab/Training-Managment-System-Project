using MediatR;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public class GetEnrollmentByIdQueryHandler(IEnrollmentService _enrollmentService)
    : IRequestHandler<GetEnrollmentByIdQuery, EnrollmentDto?>
{
    public async Task<EnrollmentDto?> Handle(GetEnrollmentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _enrollmentService.GetEnrollmentByIdAsync(request.Id, cancellationToken);
    }
}

// Backward compatibility alias
public class GetEnrollmentByIdHandler(IEnrollmentService _enrollmentService)
    : GetEnrollmentByIdQueryHandler(_enrollmentService)
{
}
