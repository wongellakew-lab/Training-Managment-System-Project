using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Transcripts;

namespace TmsApi.Infrastructure.Workers;

public class TranscriptWorker(
    TranscriptQueue _queue,
    IHubContext<TmsHub, ITmsClient> _hubContext,
    ILogger<TranscriptWorker> _logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transcript background worker started.");

        await foreach (var request in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing transcript request {RequestId} for student {StudentId}", request.RequestId, request.StudentId);

                // Simulate heavy PDF generation
                await Task.Delay(2000, stoppingToken);

                var downloadUrl = $"/api/v1/transcripts/download/{request.RequestId}.pdf";

                await _hubContext.Clients.Group($"Student_{request.StudentId}")
                    .ReceiveTranscriptStatus(request.RequestId, "Completed", downloadUrl);

                _logger.LogInformation("Transcript request {RequestId} completed successfully.", request.RequestId);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing transcript request {RequestId}", request.RequestId);

                await _hubContext.Clients.Group($"Student_{request.StudentId}")
                    .ReceiveTranscriptStatus(request.RequestId, "Failed", null);
            }
        }
    }
}
