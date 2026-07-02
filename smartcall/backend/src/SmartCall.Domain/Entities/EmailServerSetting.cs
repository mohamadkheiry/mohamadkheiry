namespace SmartCall.Domain.Entities;

public class EmailServerSetting
{
    public Guid Id { get; set; }
    public string Host { get; set; } = null!;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = null!;
    /// <summary>Encrypted at rest.</summary>
    public string Password { get; set; } = null!;
    public SmtpSecurityMode SecurityMode { get; set; } = SmtpSecurityMode.StartTls;
    public string SenderName { get; set; } = null!;
    public string SenderEmail { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
