using FluentValidation;

namespace TmsApi.Application.Assignments.Commands;

public class GradeSubmissionCommandValidator : AbstractValidator<GradeSubmissionCommand>
{
    public GradeSubmissionCommandValidator()
    {
        RuleFor(x => x.SubmissionId).GreaterThan(0).WithMessage("Valid submission ID is required.");
        RuleFor(x => x.Dto.ScoredPoints).GreaterThanOrEqualTo(0).WithMessage("Scored points cannot be negative.");
    }
}
