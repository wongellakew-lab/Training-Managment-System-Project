using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Instructors.Queries;

public record GetInstructorStatsQuery() : IRequest<InstructorStatsDto>;
