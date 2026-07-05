namespace SmartCall.Application.Common.Interfaces;

/// <summary>
/// Read/write access to AppSettings with transparent encryption of
/// sensitive keys and in-memory caching.
/// </summary>
public interface ISettingsService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string? value, bool encrypted = false, CancellationToken ct = default);
    void InvalidateCache();
}
