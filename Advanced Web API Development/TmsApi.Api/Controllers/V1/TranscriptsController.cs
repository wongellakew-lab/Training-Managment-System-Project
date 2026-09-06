using TmsApi.Application.DTOs.TranscriptDtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Api.Extensions;
using static TmsApi.Api.Diagnostics.Diagnostics;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("Transcripts")]
public class TranscriptsController(
    TranscriptQueue _queue,
    IStudentService _studentService,
    ITranscriptService _transcriptService,
    TmsDbContext _context) : ControllerBase
{

    /// <summary>
    /// Retrieves the structured academic transcript for the authenticated student.
    /// Strictly ignores any student ID passed from client when caller is a Student.
    /// </summary>
    [Authorize]
    [HttpGet("my-transcript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get the authenticated student's transcript")]
    [EndpointDescription(
        "Returns the structured academic transcript of the currently authenticated student, based " +
        "on their own student ID regardless of any client-supplied identity. Returns 401 " +
        "Unauthorized when the user is not linked to a student account and 404 Not Found when the " +
        "student's academic profile cannot be found."
    )]
    public async Task<IActionResult> GetMyTranscript(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue)
            return Unauthorized(new { message = "User not associated with a student account." });

        var transcript = await _transcriptService.GetStudentTranscriptAsync(studentId.Value, cancellationToken);
        if (transcript == null)
            return NotFound(new { message = "Student academic profile could not be found." });

        return Ok(transcript);
    }

    /// <summary>
    /// Admin / Instructor endpoint to view any student's transcript.
    /// </summary>
    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("student/{studentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get any student's transcript (admin or instructor)")]
    [EndpointDescription(
        "Returns the structured academic transcript of the specified student. Restricted to " +
        "administrators and instructors. Returns 404 Not Found when the student does not exist."
    )]
    public async Task<IActionResult> GetStudentTranscript(int studentId, CancellationToken cancellationToken)
    {
        var transcript = await _transcriptService.GetStudentTranscriptAsync(studentId, cancellationToken);
        if (transcript == null)
            return NotFound(new { message = $"Student with ID {studentId} not found." });

        return Ok(transcript);
    }

    /// <summary>
    /// Requests / queues background generation of an official academic transcript.
    /// </summary>
    [Authorize]
    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Queue an official transcript for generation")]
    [EndpointDescription(
        "Queues background generation of an official academic transcript. A student caller always " +
        "queues their own transcript regardless of any supplied student ID; administrators and " +
        "instructors may target another student. Returns 202 Accepted with a request ID, processing " +
        "status, and the eventual download URL. Returns 404 Not Found when the target student does " +
        "not exist."
    )]
    public async Task<IActionResult> RequestTranscript([FromBody] RequestTranscriptDto dto, CancellationToken cancellationToken)
    {
        int targetStudentId = dto.StudentId ?? 0;
        if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Instructor"))
        {
            var myStudentId = await User.ResolveStudentIdAsync(_context);
            if (!myStudentId.HasValue)
                return Unauthorized(new { message = "Student record not associated with authenticated user." });
            targetStudentId = myStudentId.Value;
        }

        var student = await _studentService.GetStudentByIdAsync(targetStudentId, cancellationToken);
        if (student == null)
        {
            return NotFound(new { message = $"Student with ID {targetStudentId} not found." });
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
            message = "Transcript generation has been queued. You will receive a real-time notification upon completion.",
            downloadUrl = $"/api/v1/transcripts/download/{requestId}.pdf"
        });
    }

    /// <summary>
    /// Downloads / views the official academic transcript document for the authenticated student.
    /// </summary>
    [Authorize]
    [HttpGet("download/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Download a generated transcript document")]
    [EndpointDescription(
        "Returns the official academic transcript document for the currently authenticated student " +
        "as printable HTML, matched by the file name returned from a transcript request. Returns " +
        "401 Unauthorized when the user is not linked to a student account."
    )]
    public async Task<IActionResult> DownloadTranscript(string fileName, CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue)
            return Unauthorized(new { message = "User not associated with a student account." });

        var docRef = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^4]
            : fileName;

        var html = await _transcriptService.GenerateTranscriptHtmlAsync(studentId.Value, docRef, cancellationToken);
        return Content(html, "text/html");
    }

    /// <summary>
    /// Previews the official academic transcript document directly in browser.
    /// </summary>
    [Authorize]
    [HttpGet("preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Preview the authenticated student's transcript")]
    [EndpointDescription(
        "Generates and returns a printable HTML preview of the currently authenticated student's " +
        "official academic transcript. Returns 401 Unauthorized when the user is not linked to a " +
        "student account."
    )]
    public async Task<IActionResult> PreviewMyTranscript(CancellationToken cancellationToken)
    {
        var studentId = await User.ResolveStudentIdAsync(_context);
        if (!studentId.HasValue)
            return Unauthorized(new { message = "User not associated with a student account." });

        var html = await _transcriptService.GenerateTranscriptHtmlAsync(studentId.Value, null, cancellationToken);
        return Content(html, "text/html");
    }
}
