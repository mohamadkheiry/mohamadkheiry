using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Application.Features.Admin;
using SmartCall.Domain;

namespace SmartCall.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController(IMediator mediator) : ControllerBase
{
    // ---- 9.1 AI settings ----

    [HttpGet("ai-settings")]
    public Task<AiSettingsDto> GetAiSettings(CancellationToken ct)
        => mediator.Send(new GetAiSettingsQuery(), ct);

    [HttpPut("ai-settings")]
    public async Task<IActionResult> UpdateAiSettings(UpdateAiSettingsCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("ai-settings/test")]
    public Task<ConnectionTestResult> TestAiConnection(CancellationToken ct)
        => mediator.Send(new TestAiConnectionCommand(), ct);

    // ---- 9.2 Language & i18n ----

    [HttpGet("general-settings")]
    [AllowAnonymous] // the frontend needs default language before login
    public Task<GeneralSettingsDto> GetGeneralSettings(CancellationToken ct)
        => mediator.Send(new GetGeneralSettingsQuery(), ct);

    [HttpPut("general-settings")]
    public async Task<IActionResult> UpdateGeneralSettings(UpdateGeneralSettingsCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpGet("languages")]
    public Task<List<AdminLanguageDto>> GetLanguages(CancellationToken ct)
        => mediator.Send(new GetAdminLanguagesQuery(), ct);

    [HttpPost("languages")]
    public async Task<IActionResult> UpsertLanguage(UpsertLanguageCommand command, CancellationToken ct)
        => Ok(new { id = await mediator.Send(command, ct) });

    [HttpDelete("languages/{id:guid}")]
    public async Task<IActionResult> DeleteLanguage(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteLanguageCommand(id), ct);
        return NoContent();
    }

    // ---- 9.3 Calls ----

    [HttpGet("calls")]
    public async Task<IActionResult> GetCalls([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] CallStatus? status = null, CancellationToken ct = default)
    {
        var (items, total) = await mediator.Send(new GetAdminCallsQuery(page, pageSize, status), ct);
        return Ok(new { items, total });
    }

    // ---- 9.4 Users & token usage ----

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var (items, total) = await mediator.Send(new GetAdminUsersQuery(page, pageSize), ct);
        return Ok(new { items, total });
    }

    public record SetActiveRequest(bool IsActive);

    [HttpPut("users/{userId:guid}/active")]
    public async Task<IActionResult> SetUserActive(Guid userId, SetActiveRequest request, CancellationToken ct)
    {
        await mediator.Send(new SetUserActiveCommand(userId, request.IsActive), ct);
        return NoContent();
    }

    [HttpGet("token-usage")]
    public Task<TokenUsageReportDto> GetTokenUsage([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => mediator.Send(new GetTokenUsageReportQuery(from, to), ct);

    // ---- 9.5 Typography ----

    [HttpGet("typography")]
    [AllowAnonymous] // the frontend applies fonts before login
    public Task<TypographyDto> GetTypography(CancellationToken ct)
        => mediator.Send(new GetTypographyQuery(), ct);

    [HttpPost("fonts")]
    public async Task<IActionResult> UpsertFont(UpsertFontCommand command, CancellationToken ct)
        => Ok(new { id = await mediator.Send(command, ct) });

    [HttpPost("fonts/assign")]
    public async Task<IActionResult> AssignFont(AssignFontCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    // ---- 9.6 Landing page content ----

    [HttpPut("landing-content")]
    public async Task<IActionResult> UpsertLandingContent(UpsertLandingContentCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    // ---- 9.7 SMTP ----

    [HttpGet("smtp-settings")]
    public Task<SmtpSettingsDto> GetSmtpSettings(CancellationToken ct)
        => mediator.Send(new GetSmtpSettingsQuery(), ct);

    [HttpPut("smtp-settings")]
    public async Task<IActionResult> UpdateSmtpSettings(UpdateSmtpSettingsCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    public record TestEmailRequest(string ToEmail);

    [HttpPost("smtp-settings/test")]
    public async Task<IActionResult> SendTestEmail(TestEmailRequest request, CancellationToken ct)
    {
        await mediator.Send(new SendTestEmailCommand(request.ToEmail), ct);
        return Ok(new { message = "Test email sent." });
    }
}
