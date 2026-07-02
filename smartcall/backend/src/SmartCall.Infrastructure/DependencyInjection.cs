using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Infrastructure.Installation;
using SmartCall.Infrastructure.Persistence;
using SmartCall.Infrastructure.Services;

namespace SmartCall.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var installationState = new InstallationState(contentRootPath);
        services.AddSingleton(installationState);

        services.AddDbContext<AppDbContext>(options =>
        {
            // Environment/config connection string wins; otherwise the one
            // persisted by the install wizard is used.
            var connectionString = configuration.GetConnectionString("Default")
                ?? installationState.ConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddMemoryCache();
        services.AddHttpClient("openai", c => c.Timeout = TimeSpan.FromMinutes(2));

        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IOpenAIService, OpenAIService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<InstallerService>();

        return services;
    }
}
