namespace SmartCall.Domain.Entities;

public class CallParticipant
{
    public Guid Id { get; set; }
    public Guid CallId { get; set; }
    public Call Call { get; set; } = null!;
    /// <summary>Null for guests who joined via invite link without an account.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string DisplayName { get; set; } = null!;
    public ParticipantRole Role { get; set; }
    /// <summary>ISO code of the language this participant wants to HEAR the other side in.</summary>
    public string? TargetLanguageCode { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
}
