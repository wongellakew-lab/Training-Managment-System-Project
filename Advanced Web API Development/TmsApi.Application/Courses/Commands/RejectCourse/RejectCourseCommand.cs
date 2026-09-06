using MediatR;

namespace TmsApi.Application.Courses.Commands;

public record RejectCourseCommand(int Id, string Reason) : IRequest<bool>;
