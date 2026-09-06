using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Queries;

public class GetInstructorStatsQueryHandler(IInstructorService _instructorService) : IRequestHandler<GetInstructorStatsQuery, InstructorStatsDto>
{
    public async Task<InstructorStatsDto> Handle(GetInstructorStatsQuery request, CancellationToken cancellationToken)
    {
        return await _instructorService.GetStatsAsync(cancellationToken);
    }
}
