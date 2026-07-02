using MediatR;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Translation;

public record RealtimeSessionDto(string ClientSecret, string Model, string BaseUrl, DateTimeOffset ExpiresAt);

/// <summary>
/// Realtime method: issues an ephemeral OpenAI Realtime session so the browser
/// can stream speech directly to the speech-to-speech model. The permanent API
/// key never leaves the server.
/// </summary>
public record CreateRealtimeSessionCommand(Guid CallId, string TargetLanguageCode) : IRequest<RealtimeSessionDto>;

public class CreateRealtimeSessionCommandHandler(IOpenAIService openAi)
    : IRequestHandler<CreateRealtimeSessionCommand, RealtimeSessionDto>
{
    public async Task<RealtimeSessionDto> Handle(CreateRealtimeSessionCommand request, CancellationToken ct)
    {
        var session = await openAi.CreateRealtimeSessionAsync(request.TargetLanguageCode, ct);
        return new RealtimeSessionDto(session.ClientSecret, session.Model, session.BaseUrl, session.ExpiresAt);
    }
}

/// <summary>Reported by the client after a realtime session ends (usage accounting).</summary>
public record LogRealtimeUsageCommand(Guid CallId, Guid? UserId, long InputTokens, long OutputTokens) : IRequest;

public class LogRealtimeUsageCommandHandler(IAppDbContext db, ISettingsService settings)
    : IRequestHandler<LogRealtimeUsageCommand>
{
    public async Task Handle(LogRealtimeUsageCommand request, CancellationToken ct)
    {
        db.TokenUsageLogs.Add(new Domain.Entities.TokenUsageLog
        {
            Id = Guid.NewGuid(),
            CallId = request.CallId,
            UserId = request.UserId,
            Kind = Domain.TokenUsageKind.RealtimeSpeech,
            Model = await settings.GetAsync(SettingKeys.OpenAiRealtimeModel, ct) ?? "",
            InputTokens = request.InputTokens,
            OutputTokens = request.OutputTokens,
            TotalTokens = request.InputTokens + request.OutputTokens
        });
        await db.SaveChangesAsync(ct);
    }
}
