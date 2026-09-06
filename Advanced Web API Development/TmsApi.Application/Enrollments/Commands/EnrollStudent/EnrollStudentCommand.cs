using MediatR;
using TmsApi.Application.DTOs.EnrollmentDtos;

namespace TmsApi.Application.Enrollments.Commands;

public record EnrollStudentCommand(int StudentId, int CourseId) : IRequest<EnrollmentDto>;
