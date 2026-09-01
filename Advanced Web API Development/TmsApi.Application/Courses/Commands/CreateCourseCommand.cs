using FluentValidation;
using MediatR;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public record CreateCourseCommand(
    string Code,
    string Title,
    int MaxCapacity,
    int? InstructorId = null,
    string Description = "",
    string Department = "Computer Science",
    int Credits = 3) : IRequest<CourseDto>;

public class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).WithMessage("Course code is required and must not exceed 20 characters.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150).WithMessage("Course title is required.");
        RuleFor(x => x.MaxCapacity).GreaterThan(0).WithMessage("Maximum capacity must be greater than zero.");
    }
}

public class CreateCourseHandler : IRequestHandler<CreateCourseCommand, CourseDto>
{
    private readonly ICourseService _courseService;

    public CreateCourseHandler(ICourseService courseService)
    {
        _courseService = courseService;
    }

    public async Task<CourseDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateCourseDto
        {
            Code = request.Code,
            Title = request.Title,
            Description = request.Description,
            Department = request.Department,
            Credits = request.Credits,
            MaxCapacity = request.MaxCapacity,
            InstructorId = request.InstructorId
        };
        return await _courseService.CreateCourseAsync(dto, cancellationToken);
    }
}

