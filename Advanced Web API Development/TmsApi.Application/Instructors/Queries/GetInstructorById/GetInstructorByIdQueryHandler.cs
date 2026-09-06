using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Queries;

public class GetInstructorByIdQueryHandler(IInstructorService _instructorService) : IRequestHandler<GetInstructorByIdQuery, InstructorDetailDto?>
{
    public async Task<InstructorDetailDto?> Handle(GetInstructorByIdQuery request, CancellationToken cancellationToken)
    {
        return await _instructorService.GetInstructorByIdAsync(request.Id, cancellationToken);
    }
}
