using MediatR;

namespace TmsApi.Application.Assignments.Commands;

public record DeleteAssignmentCommand(int Id) : IRequest<bool>;
