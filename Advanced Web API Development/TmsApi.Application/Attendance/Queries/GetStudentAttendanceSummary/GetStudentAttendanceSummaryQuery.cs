using MediatR;
using TmsApi.Application.DTOs.AttendanceDtos;

namespace TmsApi.Application.Attendance.Queries;

public record GetStudentAttendanceSummaryQuery(int StudentId, int CourseId) : IRequest<StudentAttendanceSummaryDto?>;
