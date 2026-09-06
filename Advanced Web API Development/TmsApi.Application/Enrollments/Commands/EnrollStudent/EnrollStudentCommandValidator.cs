using FluentValidation;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentCommandValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("Valid student ID required.");
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID required.");
    }
}

// Backward compatibility alias
public class EnrollStudentValidator : EnrollStudentCommandValidator
{
}
