using MediatR;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Translation;

public record CascadeTranslationResultDto(
    string SourceText,
    string TranslatedText,
    byte[] Audio,
    string AudioContentType);

/// <summary>
/// Cascade method: Speech → Text (STT) → translated text (MT) → Speech (TTS).
/// The intermediate texts are returned so they can be shown as live captions
/// and are fully loggable. Token usage of every step is recorded per user/call.
/// </summary>
public record CascadeTranslateAudioCommand(
    Guid CallId,
    Guid? SpeakerUserId,
    Stream Audio,
    string FileName,
    string TargetLanguageCode) : IRequest<CascadeTranslationResultDto>;

public class CascadeTranslateAudioCommandHandler(
    IAppDbContext db,
    IOpenAIService openAi,
    ISettingsService settings)
    : IRequestHandler<CascadeTranslateAudioCommand, CascadeTranslationResultDto>
{
    public async Task<CascadeTranslationResultDto> Handle(CascadeTranslateAudioCommand request, CancellationToken ct)
    {
        // 1. Speech-to-text
        var transcription = await openAi.TranscribeAsync(request.Audio, request.FileName, null, ct);

        if (string.IsNullOrWhiteSpace(transcription.Text))
            return new CascadeTranslationResultDto("", "", Array.Empty<byte>(), "audio/mpeg");

        // 2. Text translation
        var translation = await openAi.TranslateTextAsync(transcription.Text, request.TargetLanguageCode, ct);

        // 3. Text-to-speech
        var speech = await openAi.SynthesizeSpeechAsync(translation.TranslatedText, ct);

        // 4. Token accounting
        var sttModel = await settings.GetAsync(SettingKeys.OpenAiSttModel, ct) ?? "";
        var mtModel = await settings.GetAsync(SettingKeys.OpenAiTranslationModel, ct) ?? "";
        var ttsModel = await settings.GetAsync(SettingKeys.OpenAiTtsModel, ct) ?? "";

        db.TokenUsageLogs.Add(NewLog(request, TokenUsageKind.SpeechToText, sttModel, transcription.InputTokens, transcription.OutputTokens));
        db.TokenUsageLogs.Add(NewLog(request, TokenUsageKind.TextTranslation, mtModel, translation.InputTokens, translation.OutputTokens));
        db.TokenUsageLogs.Add(NewLog(request, TokenUsageKind.TextToSpeech, ttsModel, translation.TranslatedText.Length, 0));
        await db.SaveChangesAsync(ct);

        return new CascadeTranslationResultDto(transcription.Text, translation.TranslatedText, speech.Audio, speech.ContentType);
    }

    private static TokenUsageLog NewLog(CascadeTranslateAudioCommand req, TokenUsageKind kind, string model, long input, long output)
        => new()
        {
            Id = Guid.NewGuid(),
            CallId = req.CallId,
            UserId = req.SpeakerUserId,
            Kind = kind,
            Model = model,
            InputTokens = input,
            OutputTokens = output,
            TotalTokens = input + output
        };
}
