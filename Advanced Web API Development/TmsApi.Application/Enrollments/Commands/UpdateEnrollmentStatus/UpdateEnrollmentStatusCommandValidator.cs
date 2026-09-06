using FluentValidation;

namespace TmsApi.Application.Enrollments.Commands;

public class UpdateEnrollmentStatusCommandValidator : AbstractValidator<UpdateEnrollmentStatusCommand>
{
    public UpdateEnrollmentStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid enrollment ID required.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Valid enrollment status required.");
    }
}
