using MediatR;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public record GetEnrollmentByIdQuery(int Id) : IRequest<EnrollmentDto?>;

public class GetEnrollmentByIdHandler : IRequestHandler<GetEnrollmentByIdQuery, EnrollmentDto?>
{
    private readonly IEnrollmentService _enrollmentService;

    public GetEnrollmentByIdHandler(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<EnrollmentDto?> Handle(GetEnrollmentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _enrollmentService.GetEnrollmentByIdAsync(request.Id, cancellationToken);
    }
}
