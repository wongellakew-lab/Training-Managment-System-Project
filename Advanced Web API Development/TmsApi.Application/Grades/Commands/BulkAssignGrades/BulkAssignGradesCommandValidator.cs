using FluentValidation;

namespace TmsApi.Application.Grades.Commands;

public class BulkAssignGradesCommandValidator : AbstractValidator<BulkAssignGradesCommand>
{
    public BulkAssignGradesCommandValidator()
    {
        RuleFor(x => x.Dto).NotNull().WithMessage("Bulk assign request cannot be null.");
    }
}
