using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;

namespace TmsApi.Application.Students.Queries;

public record GetStudentsQuery(PagedRequest Request) : IRequest<PagedResponse<StudentDto>>;
