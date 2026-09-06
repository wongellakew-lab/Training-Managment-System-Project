using MediatR;
using TmsApi.Application.DTOs.GradeDtos;

namespace TmsApi.Application.Grades.Commands;

public record SubmitBulkGradebookCommand(BulkGradebookSubmitRequestDto Dto) : IRequest<object?>;
