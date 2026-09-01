using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Transcripts;
using static TmsApi.Api.Diagnostics.Diagnostics;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TranscriptsController : ControllerBase
{
    private readonly TranscriptQueue _queue;
    private readonly IStudentService _studentService;

    public TranscriptsController(TranscriptQueue queue, IStudentService studentService)
    {
        _queue = queue;
        _studentService = studentService;
    }

    public record RequestTranscriptDto(int StudentId);

    [Authorize]
    [HttpPost("request")]
    public async Task<IActionResult> RequestTranscript([FromBody] RequestTranscriptDto dto, CancellationToken cancellationToken)
    {
        var student = await _studentService.GetStudentByIdAsync(dto.StudentId, cancellationToken);
        if (student == null)
        {
            return NotFound(new { message = $"Student with ID {dto.StudentId} not found." });
        }

        var requestId = Guid.NewGuid().ToString("N");
        var request = new TranscriptRequest(
            requestId,
            student.Id,
            $"{student.FirstName} {student.LastName}",
            student.Email
        );

        await _queue.QueueTranscriptAsync(request, cancellationToken);
        TranscriptsCounter.Add(1);

        return Accepted(new
        {
            requestId,
            status = "Processing",
            message = "Transcript generation has been queued. You will receive a real-time notification upon completion."
        });
    }
}
