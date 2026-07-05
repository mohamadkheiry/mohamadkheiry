namespace SmartCall.Domain.Entities;

/// <summary>
/// Global key/value settings store. Sensitive values (marked IsEncrypted)
/// are encrypted at rest with AES using the application data-protection key.
/// </summary>
public class AppSetting
{
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Well-known setting keys used across the system.</summary>
public static class SettingKeys
{
    public const string OpenAiApiKey = "openai.apiKey";
    public const string OpenAiBaseUrl = "openai.baseUrl";
    public const string OpenAiSttModel = "openai.model.stt";
    public const string OpenAiTranslationModel = "openai.model.translation";
    public const string OpenAiTtsModel = "openai.model.tts";
    public const string OpenAiTtsVoice = "openai.model.ttsVoice";
    public const string OpenAiRealtimeModel = "openai.model.realtime";
    public const string ActiveTranslationMethod = "translation.activeMethod"; // cascade | realtime
    public const string DefaultDashboardLanguage = "ui.defaultLanguage";       // fa | en
    public const string AllowUserLanguageSwitch = "ui.allowLanguageSwitch";    // true | false
    public const string StunTurnServers = "webrtc.iceServers";                 // JSON array
}
