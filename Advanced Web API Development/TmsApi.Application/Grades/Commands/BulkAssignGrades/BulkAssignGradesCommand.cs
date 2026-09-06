using MediatR;
using TmsApi.Application.DTOs.GradeDtos;

namespace TmsApi.Application.Grades.Commands;

public record BulkAssignGradesCommand(BulkAssignGradesRequestDto Dto) : IRequest<List<object>>;
