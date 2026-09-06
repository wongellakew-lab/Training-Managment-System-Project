using MediatR;
using TmsApi.Application.DTOs.EnrollmentDtos;

namespace TmsApi.Application.Enrollments.Queries;

public record GetEnrollmentByIdQuery(int Id) : IRequest<EnrollmentDto?>;
