using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Application.Features.Translation;
using SmartCall.Domain.Entities;

namespace SmartCall.Api.Controllers;

[ApiController]
[Route("api/translation")]
public class TranslationController(IMediator mediator, ISettingsService settings, ICurrentUserService currentUser)
    : ControllerBase
{
    /// <summary>Which translation method the admin has activated: cascade | realtime.</summary>
    [HttpGet("method")]
    public async Task<IActionResult> GetActiveMethod(CancellationToken ct)
        => Ok(new { method = await settings.GetAsync(SettingKeys.ActiveTranslationMethod, ct) ?? "cascade" });

    /// <summary>
    /// Cascade pipeline: accepts an audio segment (multipart) and returns the
    /// source text, translated text and synthesized speech (base64).
    /// </summary>
    [HttpPost("cascade")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Cascade(
        [FromForm] IFormFile audio,
        [FromForm] Guid callId,
        [FromForm] string targetLanguage,
        CancellationToken ct)
    {
        await using var stream = audio.OpenReadStream();
        var result = await mediator.Send(new CascadeTranslateAudioCommand(
            callId, currentUser.UserId, stream, audio.FileName, targetLanguage), ct);

        return Ok(new
        {
            sourceText = result.SourceText,
            translatedText = result.TranslatedText,
            audioBase64 = Convert.ToBase64String(result.Audio),
            audioContentType = result.AudioContentType
        });
    }

    public record RealtimeSessionRequest(Guid CallId, string TargetLanguage);

    /// <summary>Realtime method: mints an ephemeral OpenAI Realtime session for the browser.</summary>
    [HttpPost("realtime/session")]
    public async Task<RealtimeSessionDto> CreateRealtimeSession(RealtimeSessionRequest request, CancellationToken ct)
        => await mediator.Send(new CreateRealtimeSessionCommand(request.CallId, request.TargetLanguage), ct);

    public record RealtimeUsageRequest(Guid CallId, long InputTokens, long OutputTokens);

    [HttpPost("realtime/usage")]
    public async Task<IActionResult> LogRealtimeUsage(RealtimeUsageRequest request, CancellationToken ct)
    {
        await mediator.Send(new LogRealtimeUsageCommand(request.CallId, currentUser.UserId, request.InputTokens, request.OutputTokens), ct);
        return NoContent();
    }
}
