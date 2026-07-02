using MediatR;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Admin;

public record GeneralSettingsDto(string DefaultLanguage, bool AllowLanguageSwitch, string? IceServersJson);

public record GetGeneralSettingsQuery : IRequest<GeneralSettingsDto>;

public class GetGeneralSettingsQueryHandler(ISettingsService settings)
    : IRequestHandler<GetGeneralSettingsQuery, GeneralSettingsDto>
{
    public async Task<GeneralSettingsDto> Handle(GetGeneralSettingsQuery request, CancellationToken ct)
        => new(
            await settings.GetAsync(SettingKeys.DefaultDashboardLanguage, ct) ?? "fa",
            (await settings.GetAsync(SettingKeys.AllowUserLanguageSwitch, ct) ?? "true") == "true",
            await settings.GetAsync(SettingKeys.StunTurnServers, ct));
}

public record UpdateGeneralSettingsCommand(string? DefaultLanguage, bool? AllowLanguageSwitch, string? IceServersJson) : IRequest;

public class UpdateGeneralSettingsCommandHandler(ISettingsService settings)
    : IRequestHandler<UpdateGeneralSettingsCommand>
{
    public async Task Handle(UpdateGeneralSettingsCommand request, CancellationToken ct)
    {
        if (request.DefaultLanguage is "fa" or "en")
            await settings.SetAsync(SettingKeys.DefaultDashboardLanguage, request.DefaultLanguage, ct: ct);
        if (request.AllowLanguageSwitch.HasValue)
            await settings.SetAsync(SettingKeys.AllowUserLanguageSwitch, request.AllowLanguageSwitch.Value ? "true" : "false", ct: ct);
        if (request.IceServersJson is not null)
            await settings.SetAsync(SettingKeys.StunTurnServers, request.IceServersJson, ct: ct);
    }
}
