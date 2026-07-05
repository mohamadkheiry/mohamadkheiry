namespace SmartCall.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>Saves a stream and returns the stored relative path.</summary>
    Task<string> SaveAsync(Stream content, string folder, string fileName, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default);
    Task AppendAsync(Stream content, string relativePath, CancellationToken ct = default);
    Task<long> GetSizeAsync(string relativePath, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}
