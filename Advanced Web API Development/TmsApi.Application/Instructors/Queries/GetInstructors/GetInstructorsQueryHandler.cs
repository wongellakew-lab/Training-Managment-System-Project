using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Queries;

public class GetInstructorsQueryHandler(IInstructorService _instructorService) : IRequestHandler<GetInstructorsQuery, PagedResponse<InstructorDto>>
{
    public async Task<PagedResponse<InstructorDto>> Handle(GetInstructorsQuery request, CancellationToken cancellationToken)
    {
        return await _instructorService.GetInstructorsAsync(request.Request, request.Department, cancellationToken);
    }
}
