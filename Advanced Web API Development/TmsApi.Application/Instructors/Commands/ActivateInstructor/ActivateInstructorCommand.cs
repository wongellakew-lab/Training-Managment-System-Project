using MediatR;

namespace TmsApi.Application.Instructors.Commands;

public record ActivateInstructorCommand(int Id) : IRequest<bool>;
