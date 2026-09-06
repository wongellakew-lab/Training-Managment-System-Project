using MediatR;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

public record UpdateEnrollmentStatusCommand(int Id, EnrollmentStatus Status) : IRequest<bool>;
