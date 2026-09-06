using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Queries;

public class GetInstructorProfileQueryHandler(IInstructorService _instructorService) : IRequestHandler<GetInstructorProfileQuery, InstructorProfileDto?>
{
    public async Task<InstructorProfileDto?> Handle(GetInstructorProfileQuery request, CancellationToken cancellationToken)
    {
        return await _instructorService.GetProfileAsync(request.UserId, cancellationToken);
    }
}
