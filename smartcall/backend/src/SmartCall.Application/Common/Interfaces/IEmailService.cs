namespace SmartCall.Application.Common.Interfaces;

public interface IEmailService
{
    /// <summary>Sends an email using the SMTP settings stored in the database.</summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>Sends a test email with explicit settings (admin "send test email" button).</summary>
    Task SendTestAsync(string host, int port, string username, string password,
        int securityMode, string senderName, string senderEmail, string toEmail, CancellationToken ct = default);
}
