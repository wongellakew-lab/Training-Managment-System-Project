using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Instructors.Queries;

public record GetInstructorsQuery(PagedRequest Request, string? Department = null) : IRequest<PagedResponse<InstructorDto>>;
