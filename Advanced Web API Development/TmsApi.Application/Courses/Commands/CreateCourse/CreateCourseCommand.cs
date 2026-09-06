using MediatR;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record CreateCourseCommand : IRequest<CourseDto>
{
    public CreateCourseDto Dto { get; init; }

    public CreateCourseCommand(CreateCourseDto dto)
    {
        Dto = dto;
    }

    public CreateCourseCommand(
        string code,
        string title,
        int maxCapacity,
        int? instructorId = null,
        string description = "",
        string department = "Computer Science",
        int credits = 3)
    {
        Dto = new CreateCourseDto
        {
            Code = code,
            Title = title,
            MaxCapacity = maxCapacity,
            InstructorId = instructorId,
            Description = description,
            Department = department,
            Credits = credits
        };
    }

    public string Code => Dto.Code;
    public string Title => Dto.Title;
    public int MaxCapacity => Dto.MaxCapacity;
    public int? InstructorId => Dto.InstructorId;
    public string Description => Dto.Description;
    public string Department => Dto.Department;
    public int Credits => Dto.Credits;
}
