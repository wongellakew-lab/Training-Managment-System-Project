using MediatR;
using TmsApi.Application.DTOs.ScheduleDtos;

namespace TmsApi.Application.Instructors.Queries;

public record GetInstructorSchedulesQuery(int Id, int? Month, int? Year, DateTime? Date) : IRequest<IEnumerable<ScheduleDto>?>;
