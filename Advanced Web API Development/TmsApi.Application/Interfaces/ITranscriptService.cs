using TmsApi.Application.DTOs.TranscriptDtos;

namespace TmsApi.Application.Interfaces;

public interface ITranscriptService
{
    Task<TranscriptDto?> GetStudentTranscriptAsync(int studentId, CancellationToken cancellationToken = default);
    Task<string> GenerateTranscriptHtmlAsync(int studentId, string? documentNumber = null, CancellationToken cancellationToken = default);
}
