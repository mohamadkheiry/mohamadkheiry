using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartCall.Application.Features.Admin;
using SmartCall.Infrastructure.Installation;

namespace SmartCall.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController(IMediator mediator, InstallationState installationState) : ControllerBase
{
    /// <summary>Landing page content (light CMS) for a given language.</summary>
    [HttpGet("landing/{language}")]
    public Task<List<LandingContentDto>> GetLanding(string language, CancellationToken ct)
        => mediator.Send(new GetLandingContentQuery(language == "en" ? "en" : "fa"), ct);

    /// <summary>Whether the install wizard should be shown.</summary>
    [HttpGet("install-status")]
    public IActionResult GetInstallStatus()
        => Ok(new { installed = installationState.IsInstalled });
}
