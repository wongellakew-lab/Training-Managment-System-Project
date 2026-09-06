using MediatR;
using TmsApi.Application.DTOs.StudentDtos;

namespace TmsApi.Application.Students.Commands;

public record UpdateStudentCommand(int Id, UpdateStudentDto Dto) : IRequest<bool>;
