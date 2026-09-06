using MediatR;
using TmsApi.Application.DTOs.AttendanceDtos;

namespace TmsApi.Application.Attendance.Queries;

public record GetCourseAttendanceStatsQuery(int CourseId) : IRequest<CourseAttendanceStatsDto>;
