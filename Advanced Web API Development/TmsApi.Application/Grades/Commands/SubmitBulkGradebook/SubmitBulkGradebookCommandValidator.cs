using FluentValidation;

namespace TmsApi.Application.Grades.Commands;

public class SubmitBulkGradebookCommandValidator : AbstractValidator<SubmitBulkGradebookCommand>
{
    public SubmitBulkGradebookCommandValidator()
    {
        RuleFor(x => x.Dto.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Dto.Grades).NotEmpty().WithMessage("At least one grade must be submitted.");
    }
}
