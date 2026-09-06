using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public class EnsureAssessmentsCommandValidator : AbstractValidator<EnsureAssessmentsCommand>
{
    public EnsureAssessmentsCommandValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
    }
}
