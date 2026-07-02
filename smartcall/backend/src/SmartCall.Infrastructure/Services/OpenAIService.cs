using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Infrastructure.Services;

/// <summary>
/// OpenAI(-compatible) client. API key, base URL and every model name are read
/// from the settings store on each request so the super admin can change them
/// at runtime — nothing is hardcoded. Works against api.openai.com as well as
/// OpenAI-compatible proxies / Azure OpenAI gateways.
/// </summary>
public class OpenAIService(IHttpClientFactory httpClientFactory, ISettingsService settings, ILogger<OpenAIService> logger)
    : IOpenAIService
{
    private const string DefaultBaseUrl = "https://api.openai.com/v1";

    private async Task<(HttpClient Client, string BaseUrl)> CreateClientAsync(CancellationToken ct)
    {
        var apiKey = await settings.GetAsync(SettingKeys.OpenAiApiKey, ct);
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured. Set it in the admin panel.");

        var baseUrl = await settings.GetAsync(SettingKeys.OpenAiBaseUrl, ct);
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = DefaultBaseUrl;

        var client = httpClientFactory.CreateClient("openai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return (client, baseUrl.TrimEnd('/'));
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, string? sourceLanguage, CancellationToken ct = default)
    {
        var (client, baseUrl) = await CreateClientAsync(ct);
        var model = await RequireModelAsync(SettingKeys.OpenAiSttModel, "STT", ct);

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(audio);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(model), "model");
        if (!string.IsNullOrEmpty(sourceLanguage))
            form.Add(new StringContent(sourceLanguage), "language");

        var response = await client.PostAsync($"{baseUrl}/audio/transcriptions", form, ct);
        await EnsureSuccessAsync(response, "transcription", ct);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
        var text = json?["text"]?.GetValue<string>() ?? "";
        var usage = json?["usage"];
        return new TranscriptionResult(
            text,
            usage?["input_tokens"]?.GetValue<long>() ?? 0,
            usage?["output_tokens"]?.GetValue<long>() ?? 0);
    }

    public async Task<TranslationResult> TranslateTextAsync(string text, string targetLanguageCode, CancellationToken ct = default)
    {
        var (client, baseUrl) = await CreateClientAsync(ct);
        var model = await RequireModelAsync(SettingKeys.OpenAiTranslationModel, "translation", ct);

        var payload = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are a professional real-time interpreter inside a live video call. " +
                              $"Translate everything the user says into the language with ISO 639-1 code '{targetLanguageCode}'. " +
                              "Keep the tone, register and meaning. Output ONLY the translation with no explanations."
                },
                new { role = "user", content = text }
            },
            temperature = 0.2
        };

        var response = await client.PostAsJsonAsync($"{baseUrl}/chat/completions", payload, ct);
        await EnsureSuccessAsync(response, "translation", ct);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
        var translated = json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()?.Trim() ?? "";
        var usage = json?["usage"];
        return new TranslationResult(
            translated,
            usage?["prompt_tokens"]?.GetValue<long>() ?? 0,
            usage?["completion_tokens"]?.GetValue<long>() ?? 0);
    }

    public async Task<SpeechResult> SynthesizeSpeechAsync(string text, CancellationToken ct = default)
    {
        var (client, baseUrl) = await CreateClientAsync(ct);
        var model = await RequireModelAsync(SettingKeys.OpenAiTtsModel, "TTS", ct);
        var voice = await settings.GetAsync(SettingKeys.OpenAiTtsVoice, ct) ?? "alloy";

        var payload = new { model, voice, input = text, response_format = "mp3" };
        var response = await client.PostAsJsonAsync($"{baseUrl}/audio/speech", payload, ct);
        await EnsureSuccessAsync(response, "speech synthesis", ct);

        var audio = await response.Content.ReadAsByteArrayAsync(ct);
        return new SpeechResult(audio, "audio/mpeg");
    }

    public async Task<RealtimeSessionInfo> CreateRealtimeSessionAsync(string targetLanguageCode, CancellationToken ct = default)
    {
        var (client, baseUrl) = await CreateClientAsync(ct);
        var model = await RequireModelAsync(SettingKeys.OpenAiRealtimeModel, "Realtime", ct);

        var payload = new
        {
            model,
            instructions =
                "You are a simultaneous interpreter in a live video call. " +
                $"Whatever you hear, repeat it translated into the language with ISO 639-1 code '{targetLanguageCode}'. " +
                "Do not answer questions, do not add commentary — only interpret."
        };

        var response = await client.PostAsJsonAsync($"{baseUrl}/realtime/sessions", payload, ct);
        await EnsureSuccessAsync(response, "realtime session", ct);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
        var secret = json?["client_secret"]?["value"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Realtime session did not return a client secret.");
        var expiresAt = json?["client_secret"]?["expires_at"]?.GetValue<long>() ?? 0;

        return new RealtimeSessionInfo(
            secret,
            model,
            baseUrl,
            expiresAt > 0 ? DateTimeOffset.FromUnixTimeSeconds(expiresAt) : DateTimeOffset.UtcNow.AddMinutes(1));
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var (client, baseUrl) = await CreateClientAsync(ct);
            var response = await client.GetAsync($"{baseUrl}/models", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                return new ConnectionTestResult(false, $"HTTP {(int)response.StatusCode}: {Truncate(body)}", []);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct);
            var models = json?["data"]?.AsArray()
                .Select(m => m?["id"]?.GetValue<string>())
                .Where(id => id is not null)
                .Select(id => id!)
                .OrderBy(id => id)
                .ToList() ?? [];

            return new ConnectionTestResult(true, "Connection successful.", models);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenAI connection test failed");
            return new ConnectionTestResult(false, ex.Message, []);
        }
    }

    private async Task<string> RequireModelAsync(string key, string stage, CancellationToken ct)
        => await settings.GetAsync(key, ct)
           ?? throw new InvalidOperationException($"No {stage} model configured. Set it in the admin panel.");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"OpenAI {operation} failed with HTTP {(int)response.StatusCode}: {Truncate(body)}");
    }

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500];
}
