using FluentValidation;

namespace TmsApi.Application.Instructors.Commands;

public class UpdateInstructorCommandValidator : AbstractValidator<UpdateInstructorCommand>
{
    public UpdateInstructorCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid instructor ID is required.");
        RuleFor(x => x.Dto).NotNull().WithMessage("Update data is required.");
        When(x => x.Dto != null, () =>
        {
            RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("Last name is required.");
        });
    }
}
