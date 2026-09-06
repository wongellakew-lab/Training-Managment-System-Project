using FluentValidation;

namespace TmsApi.Application.Students.Commands;

public class DeleteStudentCommandValidator : AbstractValidator<DeleteStudentCommand>
{
    public DeleteStudentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid student ID is required.");
    }
}
