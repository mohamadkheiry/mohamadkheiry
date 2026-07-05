namespace SmartCall.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsSuperAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CallParticipant> Participations { get; set; } = new List<CallParticipant>();
    public ICollection<TokenUsageLog> TokenUsages { get; set; } = new List<TokenUsageLog>();
}
