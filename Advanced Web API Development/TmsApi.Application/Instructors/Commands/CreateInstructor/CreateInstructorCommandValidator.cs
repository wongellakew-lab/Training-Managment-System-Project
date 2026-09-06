using FluentValidation;

namespace TmsApi.Application.Instructors.Commands;

public class CreateInstructorCommandValidator : AbstractValidator<CreateInstructorCommand>
{
    public CreateInstructorCommandValidator()
    {
        RuleFor(x => x.Dto).NotNull().WithMessage("Instructor data is required.");
        When(x => x.Dto != null, () =>
        {
            RuleFor(x => x.Dto.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

            RuleFor(x => x.Dto.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

            RuleFor(x => x.Dto.EmployeeNumber)
                .MaximumLength(50).WithMessage("Employee number cannot exceed 50 characters.");

            RuleFor(x => x.Dto.Department)
                .MaximumLength(100).WithMessage("Department cannot exceed 100 characters.");
        });
    }
}
