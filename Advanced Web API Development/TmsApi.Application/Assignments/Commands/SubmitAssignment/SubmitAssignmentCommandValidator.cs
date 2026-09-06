using FluentValidation;

namespace TmsApi.Application.Assignments.Commands;

public class SubmitAssignmentCommandValidator : AbstractValidator<SubmitAssignmentCommand>
{
    public SubmitAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId).GreaterThan(0).WithMessage("Valid assignment ID is required.");
        RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("Valid student ID is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Submission details are required.");
    }
}
