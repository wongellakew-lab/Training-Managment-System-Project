using MediatR;

namespace TmsApi.Application.Students.Commands;

public record WithdrawFromCourseCommand(int StudentId, int CourseId) : IRequest<bool>;
