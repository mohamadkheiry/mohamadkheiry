using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;
using SmartCall.Infrastructure.Persistence;

namespace SmartCall.Infrastructure.Installation;

public record DbConnectionInfo(string Host, int Port, string Database, string Username, string Password);
public record InstallRequest(DbConnectionInfo Db, string AdminEmail, string AdminPassword, string AdminDisplayName);

public class InstallerService(InstallationState state, IPasswordHasher hasher, ILogger<InstallerService> logger)
{
    public static string BuildConnectionString(DbConnectionInfo db)
        => new NpgsqlConnectionStringBuilder
        {
            Host = db.Host,
            Port = db.Port,
            Database = db.Database,
            Username = db.Username,
            Password = db.Password
        }.ConnectionString;

    /// <summary>"Test connection" step of the install wizard.</summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(DbConnectionInfo db, CancellationToken ct)
    {
        try
        {
            await using var conn = new NpgsqlConnection(BuildConnectionString(db));
            await conn.OpenAsync(ct);
            return (true, "Database connection successful.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Fresh install: runs all migrations, seeds defaults and creates the
    /// initial super admin account.
    /// </summary>
    public async Task FreshInstallAsync(InstallRequest request, CancellationToken ct)
    {
        var connectionString = BuildConnectionString(request.Db);
        await using var db = CreateContext(connectionString);

        logger.LogInformation("Running initial migrations…");
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync(ct);
        else
            // No compiled migrations (e.g. dev build before `dotnet ef migrations add`):
            // create the schema directly from the model.
            await db.Database.EnsureCreatedAsync(ct);

        if (!await db.Users.AnyAsync(u => u.IsSuperAdmin, ct))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = request.AdminEmail.Trim().ToLowerInvariant(),
                PasswordHash = hasher.Hash(request.AdminPassword),
                DisplayName = request.AdminDisplayName.Trim(),
                IsSuperAdmin = true
            });
        }

        await SeedDefaultsAsync(db, ct);
        await db.SaveChangesAsync(ct);

        state.MarkInstalled(connectionString);
        logger.LogInformation("Fresh install completed.");
    }

    /// <summary>
    /// "Deploy new version" path: the database already has data — connect to it
    /// and apply ONLY the pending (incremental) migrations. Nothing is dropped.
    /// </summary>
    public async Task UpgradeAsync(DbConnectionInfo dbInfo, CancellationToken ct)
    {
        var connectionString = BuildConnectionString(dbInfo);
        await using var db = CreateContext(connectionString);

        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        logger.LogInformation("Applying {Count} pending migration(s): {Names}", pending.Count, string.Join(", ", pending));
        await db.Database.MigrateAsync(ct);

        state.MarkInstalled(connectionString);
        logger.LogInformation("Upgrade completed without data loss.");
    }

    private static AppDbContext CreateContext(string connectionString)
        => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);

    private static async Task SeedDefaultsAsync(AppDbContext db, CancellationToken ct)
    {
        if (!await db.TranslationLanguages.AnyAsync(ct))
            db.TranslationLanguages.AddRange(SeedData.DefaultLanguages());

        if (!await db.Fonts.AnyAsync(ct))
            db.Fonts.AddRange(SeedData.DefaultFonts());

        if (!await db.LandingPageContents.AnyAsync(ct))
            db.LandingPageContents.AddRange(SeedData.DefaultLandingContent());

        if (!await db.AppSettings.AnyAsync(ct))
            db.AppSettings.AddRange(SeedData.DefaultSettings());
    }
}
