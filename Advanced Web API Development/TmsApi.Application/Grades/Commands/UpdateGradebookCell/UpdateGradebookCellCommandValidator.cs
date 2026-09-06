using FluentValidation;

namespace TmsApi.Application.Grades.Commands;

public class UpdateGradebookCellCommandValidator : AbstractValidator<UpdateGradebookCellCommand>
{
    public UpdateGradebookCellCommandValidator()
    {
        RuleFor(x => x.Dto.CourseId).GreaterThan(0).WithMessage("Valid course ID is required.");
        RuleFor(x => x.Dto.StudentId).GreaterThan(0).WithMessage("Valid student ID is required.");
        RuleFor(x => x.Dto.AssignmentId).GreaterThan(0).WithMessage("Valid assignment ID is required.");
    }
}
