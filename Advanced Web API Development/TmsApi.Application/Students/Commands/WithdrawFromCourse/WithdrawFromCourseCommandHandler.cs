using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Students.Commands;

public class WithdrawFromCourseCommandHandler(IStudentService _studentService) : IRequestHandler<WithdrawFromCourseCommand, bool>
{
    public async Task<bool> Handle(WithdrawFromCourseCommand request, CancellationToken cancellationToken)
    {
        return await _studentService.WithdrawFromCourseAsync(request.StudentId, request.CourseId, cancellationToken);
    }
}
