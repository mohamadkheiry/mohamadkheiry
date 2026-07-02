namespace SmartCall.Domain.Entities;

public class Call
{
    public Guid Id { get; set; }
    /// <summary>Unique token used in the invite link (e.g. /call/{LinkCode}).</summary>
    public string LinkCode { get; set; } = null!;
    public Guid CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public CallStatus Status { get; set; } = CallStatus.Waiting;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public ICollection<CallParticipant> Participants { get; set; } = new List<CallParticipant>();
    public ICollection<CallRecording> Recordings { get; set; } = new List<CallRecording>();
    public ICollection<TokenUsageLog> TokenUsages { get; set; } = new List<TokenUsageLog>();
}
