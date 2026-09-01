using System.Threading.Channels;

namespace TmsApi.Infrastructure.Transcripts;

public record TranscriptRequest(string RequestId, int StudentId, string StudentName, string UserEmail);

public class TranscriptQueue
{
    private readonly Channel<TranscriptRequest> _channel;

    public TranscriptQueue()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<TranscriptRequest>(options);
    }

    public async ValueTask QueueTranscriptAsync(TranscriptRequest request, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<TranscriptRequest> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
