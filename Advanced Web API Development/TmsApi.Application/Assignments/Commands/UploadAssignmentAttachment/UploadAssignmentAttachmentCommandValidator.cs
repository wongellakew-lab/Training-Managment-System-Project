using FluentValidation;

namespace TmsApi.Application.Assignments.Commands;

public class UploadAssignmentAttachmentCommandValidator : AbstractValidator<UploadAssignmentAttachmentCommand>
{
    public UploadAssignmentAttachmentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid assignment ID is required.");
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
    }
}
