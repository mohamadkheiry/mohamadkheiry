using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Application.Features.Calls;
using SmartCall.Domain.Entities;

namespace SmartCall.Api.Controllers;

[ApiController]
[Route("api/calls")]
public class CallsController(IMediator mediator, ISettingsService settings) : ControllerBase
{
    /// <summary>Only logged-in users can create a call / invite link.</summary>
    [Authorize]
    [HttpPost]
    public async Task<CreateCallResultDto> Create(CancellationToken ct)
        => await mediator.Send(new CreateCallCommand(), ct);

    [HttpGet("{linkCode}")]
    public async Task<CallInfoDto> GetByLink(string linkCode, CancellationToken ct)
        => await mediator.Send(new GetCallByLinkQuery(linkCode), ct);

    public record JoinRequest(string DisplayName);

    /// <summary>Guests may join via the invite link without an account.</summary>
    [HttpPost("{linkCode}/join")]
    public async Task<JoinCallResultDto> Join(string linkCode, JoinRequest request, CancellationToken ct)
        => await mediator.Send(new JoinCallCommand(linkCode, request.DisplayName), ct);

    [HttpPost("participants/{participantId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid participantId, CancellationToken ct)
    {
        await mediator.Send(new LeaveCallCommand(participantId), ct);
        return NoContent();
    }

    public record SetLanguageRequest(string LanguageCode);

    [HttpPut("participants/{participantId:guid}/language")]
    public async Task<IActionResult> SetLanguage(Guid participantId, SetLanguageRequest request, CancellationToken ct)
    {
        await mediator.Send(new SetParticipantLanguageCommand(participantId, request.LanguageCode), ct);
        return NoContent();
    }

    [HttpGet("languages")]
    public async Task<List<LanguageDto>> GetLanguages(CancellationToken ct)
        => await mediator.Send(new GetActiveLanguagesQuery(), ct);

    /// <summary>ICE (STUN/TURN) servers configured by the admin.</summary>
    [HttpGet("ice-servers")]
    public async Task<ContentResult> GetIceServers(CancellationToken ct)
    {
        var json = await settings.GetAsync(SettingKeys.StunTurnServers, ct)
            ?? """[{"urls":"stun:stun.l.google.com:19302"}]""";
        return Content(json, "application/json");
    }
}
