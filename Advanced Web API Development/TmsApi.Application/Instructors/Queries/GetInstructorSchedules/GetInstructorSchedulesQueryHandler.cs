using MediatR;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Queries;

public class GetInstructorSchedulesQueryHandler(IInstructorService _instructorService, IScheduleService _scheduleService)
    : IRequestHandler<GetInstructorSchedulesQuery, IEnumerable<ScheduleDto>?>
{
    public async Task<IEnumerable<ScheduleDto>?> Handle(GetInstructorSchedulesQuery request, CancellationToken cancellationToken)
    {
        var instructor = await _instructorService.GetInstructorByIdAsync(request.Id, cancellationToken);
        if (instructor == null) return null;
        return await _scheduleService.GetInstructorSchedulesAsync(request.Id, request.Month, request.Year, request.Date, cancellationToken);
    }
}
