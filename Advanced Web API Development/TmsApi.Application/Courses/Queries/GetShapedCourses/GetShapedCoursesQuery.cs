using MediatR;
using TmsApi.Application.DTOs;

namespace TmsApi.Application.Courses.Queries;

public record GetShapedCoursesQuery(PagedRequest Request) : IRequest<PagedResponse<System.Dynamic.ExpandoObject>>;
