using MediatR;

namespace TmsApi.Application.Courses.Commands;

public record ApproveCourseCommand(int Id) : IRequest<bool>;
