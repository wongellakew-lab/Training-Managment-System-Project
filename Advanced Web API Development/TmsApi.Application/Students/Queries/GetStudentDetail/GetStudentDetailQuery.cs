using MediatR;
using TmsApi.Application.DTOs.StudentDtos;

namespace TmsApi.Application.Students.Queries;

public record GetStudentDetailQuery(int Id) : IRequest<AdminStudentDetailDto?>;
