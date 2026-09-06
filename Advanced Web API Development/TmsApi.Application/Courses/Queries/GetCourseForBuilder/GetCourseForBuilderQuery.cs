using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Queries;

public record GetCourseForBuilderQuery(int Id) : IRequest<CourseBuilderDto?>;
