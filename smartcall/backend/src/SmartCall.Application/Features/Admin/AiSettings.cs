using MediatR;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Admin;

public record AiSettingsDto(
    bool HasApiKey,
    string? BaseUrl,
    string? SttModel,
    string? TranslationModel,
    string? TtsModel,
    string? TtsVoice,
    string? RealtimeModel,
    string ActiveMethod);

public record GetAiSettingsQuery : IRequest<AiSettingsDto>;

public class GetAiSettingsQueryHandler(ISettingsService settings) : IRequestHandler<GetAiSettingsQuery, AiSettingsDto>
{
    public async Task<AiSettingsDto> Handle(GetAiSettingsQuery request, CancellationToken ct)
        => new(
            !string.IsNullOrEmpty(await settings.GetAsync(SettingKeys.OpenAiApiKey, ct)),
            await settings.GetAsync(SettingKeys.OpenAiBaseUrl, ct),
            await settings.GetAsync(SettingKeys.OpenAiSttModel, ct),
            await settings.GetAsync(SettingKeys.OpenAiTranslationModel, ct),
            await settings.GetAsync(SettingKeys.OpenAiTtsModel, ct),
            await settings.GetAsync(SettingKeys.OpenAiTtsVoice, ct),
            await settings.GetAsync(SettingKeys.OpenAiRealtimeModel, ct),
            await settings.GetAsync(SettingKeys.ActiveTranslationMethod, ct) ?? "cascade");
}

/// <summary>
/// Updates AI configuration. All model names are editable fields (nothing is
/// hardcoded); the API key is stored encrypted and never echoed back.
/// </summary>
public record UpdateAiSettingsCommand(
    string? ApiKey,          // null = keep current
    string? BaseUrl,
    string? SttModel,
    string? TranslationModel,
    string? TtsModel,
    string? TtsVoice,
    string? RealtimeModel,
    string? ActiveMethod) : IRequest;

public class UpdateAiSettingsCommandHandler(ISettingsService settings) : IRequestHandler<UpdateAiSettingsCommand>
{
    public async Task Handle(UpdateAiSettingsCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
            await settings.SetAsync(SettingKeys.OpenAiApiKey, request.ApiKey.Trim(), encrypted: true, ct);
        if (request.BaseUrl is not null)
            await settings.SetAsync(SettingKeys.OpenAiBaseUrl, request.BaseUrl.Trim().TrimEnd('/'), ct: ct);
        if (request.SttModel is not null)
            await settings.SetAsync(SettingKeys.OpenAiSttModel, request.SttModel.Trim(), ct: ct);
        if (request.TranslationModel is not null)
            await settings.SetAsync(SettingKeys.OpenAiTranslationModel, request.TranslationModel.Trim(), ct: ct);
        if (request.TtsModel is not null)
            await settings.SetAsync(SettingKeys.OpenAiTtsModel, request.TtsModel.Trim(), ct: ct);
        if (request.TtsVoice is not null)
            await settings.SetAsync(SettingKeys.OpenAiTtsVoice, request.TtsVoice.Trim(), ct: ct);
        if (request.RealtimeModel is not null)
            await settings.SetAsync(SettingKeys.OpenAiRealtimeModel, request.RealtimeModel.Trim(), ct: ct);
        if (request.ActiveMethod is "cascade" or "realtime")
            await settings.SetAsync(SettingKeys.ActiveTranslationMethod, request.ActiveMethod, ct: ct);
    }
}

public record TestAiConnectionCommand : IRequest<ConnectionTestResult>;

public class TestAiConnectionCommandHandler(IOpenAIService openAi)
    : IRequestHandler<TestAiConnectionCommand, ConnectionTestResult>
{
    public Task<ConnectionTestResult> Handle(TestAiConnectionCommand request, CancellationToken ct)
        => openAi.TestConnectionAsync(ct);
}
