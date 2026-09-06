using MediatR;

namespace TmsApi.Application.Instructors.Commands;

public record DeleteInstructorCommand(int Id) : IRequest<bool>;
