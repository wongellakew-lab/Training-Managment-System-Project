using MediatR;
using TmsApi.Application.DTOs.AttendanceDtos;

namespace TmsApi.Application.Attendance.Queries;

public record GetCourseAttendanceSessionQuery(int CourseId, DateTime? Date) : IRequest<CourseAttendanceSessionDto?>;
