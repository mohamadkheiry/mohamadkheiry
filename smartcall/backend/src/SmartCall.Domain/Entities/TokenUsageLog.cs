namespace SmartCall.Domain.Entities;

public class TokenUsageLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? CallId { get; set; }
    public Call? Call { get; set; }
    public TokenUsageKind Kind { get; set; }
    public string Model { get; set; } = null!;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
