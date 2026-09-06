using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Instructors.Queries;

public record GetInstructorProfileQuery(string UserId) : IRequest<InstructorProfileDto?>;
