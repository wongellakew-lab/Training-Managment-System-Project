using MediatR;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentCommandHandler(IEnrollmentService _enrollmentService)
    : IRequestHandler<EnrollStudentCommand, EnrollmentDto>
{
    public async Task<EnrollmentDto> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateEnrollmentDto
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId
        };
        return await _enrollmentService.CreateEnrollmentAsync(dto, cancellationToken);
    }
}

// Backward compatibility alias
public class EnrollStudentHandler(IEnrollmentService _enrollmentService)
    : EnrollStudentCommandHandler(_enrollmentService)
{
}
