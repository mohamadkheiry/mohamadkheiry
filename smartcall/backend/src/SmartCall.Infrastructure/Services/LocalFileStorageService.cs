using Microsoft.Extensions.Configuration;
using SmartCall.Application.Common.Interfaces;

namespace SmartCall.Infrastructure.Services;

/// <summary>
/// Local-disk storage for recordings, fonts and landing media. The root folder
/// is configurable; swap this implementation for an S3-compatible one without
/// touching callers.
/// </summary>
public class LocalFileStorageService(IConfiguration configuration) : IFileStorageService
{
    private readonly string _root = Path.GetFullPath(configuration["Storage:RootPath"] ?? "storage");

    public async Task<string> SaveAsync(Stream content, string folder, string fileName, CancellationToken ct = default)
    {
        var relative = Path.Combine(folder, fileName).Replace('\\', '/');
        var full = ResolveSafe(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
        return relative;
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default)
        => Task.FromResult<Stream>(File.OpenRead(ResolveSafe(relativePath)));

    public async Task AppendAsync(Stream content, string relativePath, CancellationToken ct = default)
    {
        var full = ResolveSafe(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = new FileStream(full, FileMode.Append, FileAccess.Write, FileShare.Read);
        await content.CopyToAsync(fs, ct);
    }

    public Task<long> GetSizeAsync(string relativePath, CancellationToken ct = default)
        => Task.FromResult(new FileInfo(ResolveSafe(relativePath)).Length);

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var full = ResolveSafe(relativePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    private string ResolveSafe(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!full.StartsWith(_root, StringComparison.Ordinal))
            throw new InvalidOperationException("Path escapes the storage root.");
        return full;
    }
}
