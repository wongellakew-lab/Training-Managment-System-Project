using MediatR;

namespace TmsApi.Application.Students.Commands;

public record DeleteStudentCommand(int Id) : IRequest<bool>;
