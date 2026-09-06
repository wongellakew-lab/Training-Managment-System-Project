using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Queries;

public record GetCourseByIdQuery(int Id) : IRequest<CourseDto?>;
