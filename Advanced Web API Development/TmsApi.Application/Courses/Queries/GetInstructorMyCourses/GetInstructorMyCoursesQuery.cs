using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;

namespace TmsApi.Application.Courses.Queries;

public record GetInstructorMyCoursesQuery(int InstructorId) : IRequest<List<InstructorCourseDto>>;
