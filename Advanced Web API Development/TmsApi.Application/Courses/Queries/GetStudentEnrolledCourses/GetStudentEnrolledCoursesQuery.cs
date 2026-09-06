using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Queries;

public record GetStudentEnrolledCoursesQuery(int StudentId) : IRequest<List<StudentEnrolledCourseDto>>;
