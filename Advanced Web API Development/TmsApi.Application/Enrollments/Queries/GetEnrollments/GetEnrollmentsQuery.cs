using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.EnrollmentDtos;

namespace TmsApi.Application.Enrollments.Queries;

public record GetEnrollmentsQuery(
    int Page,
    int PageSize,
    int? StudentId = null,
    int? CourseId = null,
    string? Search = null,
    string? Status = null,
    string? Category = null) : IRequest<PagedResponse<EnrollmentDto>>;
