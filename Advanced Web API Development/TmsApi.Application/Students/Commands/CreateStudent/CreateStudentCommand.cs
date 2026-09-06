using MediatR;
using TmsApi.Application.DTOs.StudentDtos;

namespace TmsApi.Application.Students.Commands;

public record CreateStudentCommand(CreateStudentDto Dto) : IRequest<StudentDto>;
