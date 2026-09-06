using MediatR;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Assignments.Queries;

public record GetAssignmentsByCourseQuery(int CourseId) : IRequest<List<AssignmentDto>>;
