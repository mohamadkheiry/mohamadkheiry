namespace SmartCall.Application.Common.Interfaces;

public record TranscriptionResult(string Text, long InputTokens, long OutputTokens);
public record TranslationResult(string TranslatedText, long InputTokens, long OutputTokens);
public record SpeechResult(byte[] Audio, string ContentType);
public record RealtimeSessionInfo(string ClientSecret, string Model, string BaseUrl, DateTimeOffset ExpiresAt);
public record ConnectionTestResult(bool Success, string Message, IReadOnlyList<string> AvailableModels);

/// <summary>
/// Abstraction over the OpenAI(-compatible) API. API key, base URL and model
/// names are read from AppSettings at call time — never hardcoded — so the
/// super admin can change them without redeploying.
/// </summary>
public interface IOpenAIService
{
    /// <summary>Cascade step 1: speech to text.</summary>
    Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, string? sourceLanguage, CancellationToken ct = default);

    /// <summary>Cascade step 2: translate text to the target language.</summary>
    Task<TranslationResult> TranslateTextAsync(string text, string targetLanguageCode, CancellationToken ct = default);

    /// <summary>Cascade step 3: text to speech.</summary>
    Task<SpeechResult> SynthesizeSpeechAsync(string text, CancellationToken ct = default);

    /// <summary>Creates an ephemeral Realtime API session for direct browser connection.</summary>
    Task<RealtimeSessionInfo> CreateRealtimeSessionAsync(string targetLanguageCode, CancellationToken ct = default);

    /// <summary>Validates API key / base URL / configured models ("Test connection" button).</summary>
    Task<ConnectionTestResult> TestConnectionAsync(CancellationToken ct = default);
}
