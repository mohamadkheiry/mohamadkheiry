using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;
using SmartCall.Infrastructure.Persistence;

namespace SmartCall.Infrastructure.Services;

public class SettingsService(AppDbContext db, IEncryptionService encryption, IMemoryCache cache) : ISettingsService
{
    private const string CachePrefix = "appsetting:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        if (cache.TryGetValue(CachePrefix + key, out string? cached))
            return cached;

        var setting = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
        var value = setting?.Value;
        if (value is not null && setting!.IsEncrypted)
            value = encryption.Decrypt(value);

        cache.Set(CachePrefix + key, value, CacheTtl);
        return value;
    }

    public async Task SetAsync(string key, string? value, bool encrypted = false, CancellationToken ct = default)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        var stored = value is not null && encrypted ? encryption.Encrypt(value) : value;

        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = stored, IsEncrypted = encrypted });
        }
        else
        {
            setting.Value = stored;
            setting.IsEncrypted = encrypted;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        cache.Remove(CachePrefix + key);
    }

    public void InvalidateCache()
    {
        // MemoryCache has no clear-by-prefix; entries expire within CacheTtl anyway.
    }
}
