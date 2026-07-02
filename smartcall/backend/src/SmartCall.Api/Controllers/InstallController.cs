using Microsoft.AspNetCore.Mvc;
using SmartCall.Infrastructure.Installation;

namespace SmartCall.Api.Controllers;

/// <summary>
/// WordPress-style install wizard endpoints. Available only while the app is
/// not yet installed; afterwards only the upgrade endpoint stays usable.
/// </summary>
[ApiController]
[Route("api/install")]
public class InstallController(InstallerService installer, InstallationState state) : ControllerBase
{
    /// <summary>Step 1: validate database connection info.</summary>
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection(DbConnectionInfo db, CancellationToken ct)
    {
        var (success, message) = await installer.TestConnectionAsync(db, ct);
        return Ok(new { success, message });
    }

    /// <summary>
    /// Step 2 (fresh install): run initial migrations, seed defaults and create
    /// the first super admin account.
    /// </summary>
    [HttpPost("fresh")]
    public async Task<IActionResult> FreshInstall(InstallRequest request, CancellationToken ct)
    {
        if (state.IsInstalled)
            return Conflict(new { error = "Application is already installed. Use the upgrade option instead." });

        await installer.FreshInstallAsync(request, ct);
        return Ok(new { message = "Installation completed." });
    }

    /// <summary>
    /// "Deploy new version": the database already contains data — apply only
    /// the new (incremental) migrations without touching existing data.
    /// </summary>
    [HttpPost("upgrade")]
    public async Task<IActionResult> Upgrade(DbConnectionInfo db, CancellationToken ct)
    {
        await installer.UpgradeAsync(db, ct);
        return Ok(new { message = "Upgrade completed. Existing data was preserved." });
    }
}
