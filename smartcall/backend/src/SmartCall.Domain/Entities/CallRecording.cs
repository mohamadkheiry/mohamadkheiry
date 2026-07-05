namespace SmartCall.Domain.Entities;

public class CallRecording
{
    public Guid Id { get; set; }
    public Guid CallId { get; set; }
    public Call Call { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string ContentType { get; set; } = "video/webm";
    public long FileSizeBytes { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
