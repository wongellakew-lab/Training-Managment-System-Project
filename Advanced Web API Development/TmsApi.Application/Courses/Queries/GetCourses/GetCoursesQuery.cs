using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Queries;

public record GetCoursesQuery(PagedRequest Request) : IRequest<PagedResponse<CourseDto>>;
