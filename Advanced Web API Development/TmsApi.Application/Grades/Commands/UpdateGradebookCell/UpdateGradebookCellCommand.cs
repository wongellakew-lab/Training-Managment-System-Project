using MediatR;
using TmsApi.Application.DTOs.GradeDtos;

namespace TmsApi.Application.Grades.Commands;

public record UpdateGradebookCellCommand(GradebookCellRequestDto Dto) : IRequest<object?>;
