using FluentValidation;
using MediatR;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Commands;

public record EnrollStudentCommand(int StudentId, int CourseId) : IRequest<EnrollmentDto>;

public class EnrollStudentValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("Valid student ID required.");
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID required.");
    }
}

public class EnrollStudentHandler : IRequestHandler<EnrollStudentCommand, EnrollmentDto>
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollStudentHandler(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

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
