using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Application.Features.Calls;

namespace SmartCall.Api.Controllers;

[ApiController]
[Route("api/recordings")]
public class RecordingsController(IMediator mediator, IFileStorageService storage) : ControllerBase
{
    public record StartRequest(Guid CallId);

    [HttpPost("start")]
    public async Task<IActionResult> Start(StartRequest request, CancellationToken ct)
    {
        var id = await mediator.Send(new StartRecordingCommand(request.CallId), ct);
        return Ok(new { recordingId = id });
    }

    /// <summary>Receives MediaRecorder chunks from the browser and appends them to the file.</summary>
    [HttpPost("{recordingId:guid}/chunk")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> AppendChunk(Guid recordingId, CancellationToken ct)
    {
        await mediator.Send(new AppendRecordingChunkCommand(recordingId, Request.Body), ct);
        return NoContent();
    }

    [HttpPost("{recordingId:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid recordingId, CancellationToken ct)
    {
        await mediator.Send(new FinalizeRecordingCommand(recordingId), ct);
        return NoContent();
    }

    /// <summary>Playback/download — super admin only.</summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("{recordingId:guid}/download")]
    public async Task<IActionResult> Download(Guid recordingId, [FromServices] IAppDbContext db, CancellationToken ct)
    {
        var recording = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(db.CallRecordings, r => r.Id == recordingId, ct);
        if (recording is null) return NotFound();

        var stream = await storage.OpenReadAsync(recording.FilePath, ct);
        return File(stream, recording.ContentType, enableRangeProcessing: true);
    }
}
