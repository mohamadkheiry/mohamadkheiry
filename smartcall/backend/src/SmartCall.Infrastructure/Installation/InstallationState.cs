using System.Text.Json;

namespace SmartCall.Infrastructure.Installation;

/// <summary>
/// Tracks whether the app has been installed (WordPress-style). The connection
/// string chosen in the install wizard is persisted to a JSON file next to the
/// app so it survives restarts; environment configuration takes precedence.
/// </summary>
public class InstallationState
{
    private readonly string _filePath;

    public InstallationState(string contentRootPath)
    {
        _filePath = Path.Combine(contentRootPath, "smartcall.install.json");
        if (File.Exists(_filePath))
        {
            var doc = JsonSerializer.Deserialize<PersistedState>(File.ReadAllText(_filePath));
            ConnectionString = doc?.ConnectionString;
            IsInstalled = doc?.IsInstalled ?? false;
        }
    }

    public bool IsInstalled { get; private set; }
    public string? ConnectionString { get; private set; }

    public void MarkInstalled(string connectionString)
    {
        ConnectionString = connectionString;
        IsInstalled = true;
        Persist();
    }

    private void Persist()
        => File.WriteAllText(_filePath, JsonSerializer.Serialize(
            new PersistedState(IsInstalled, ConnectionString),
            new JsonSerializerOptions { WriteIndented = true }));

    private record PersistedState(bool IsInstalled, string? ConnectionString);
}
