using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Queries;

public record GetStudentCoursesStatisticsQuery(int StudentId) : IRequest<StudentCoursesStatisticsDto>;
