using FluentValidation;

namespace TmsApi.Application.Grades.Commands;

public class AssignGradeCommandValidator : AbstractValidator<AssignGradeCommand>
{
    public AssignGradeCommandValidator()
    {
        RuleFor(x => x.Dto.EnrollmentId).GreaterThan(0).WithMessage("Valid enrollment ID is required.");
    }
}
