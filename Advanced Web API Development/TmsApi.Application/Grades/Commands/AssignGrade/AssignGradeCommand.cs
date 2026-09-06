using MediatR;
using TmsApi.Application.DTOs.GradeDtos;

namespace TmsApi.Application.Grades.Commands;

public record AssignGradeCommand(AssignGradeRequestDto Dto) : IRequest<object?>;
