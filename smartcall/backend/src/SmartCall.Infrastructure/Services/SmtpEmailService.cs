using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain;
using SmartCall.Infrastructure.Persistence;

namespace SmartCall.Infrastructure.Services;

public class SmtpEmailService(AppDbContext db, IEncryptionService encryption) : IEmailService
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var s = await db.EmailServerSettings.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("SMTP settings are not configured. Set them in the admin panel.");

        await SendCoreAsync(s.Host, s.Port, s.Username, encryption.Decrypt(s.Password),
            s.SecurityMode, s.SenderName, s.SenderEmail, toEmail, subject, htmlBody, ct);
    }

    public Task SendTestAsync(string host, int port, string username, string password,
        int securityMode, string senderName, string senderEmail, string toEmail, CancellationToken ct = default)
        => SendCoreAsync(host, port, username, password, (SmtpSecurityMode)securityMode,
            senderName, senderEmail, toEmail,
            "SmartCall — ایمیل آزمایشی / Test email",
            "<p>اتصال SMTP با موفقیت برقرار شد.</p><p>SMTP connection is working.</p>", ct);

    private static async Task SendCoreAsync(string host, int port, string username, string password,
        SmtpSecurityMode securityMode, string senderName, string senderEmail,
        string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        var socketOptions = securityMode switch
        {
            SmtpSecurityMode.Ssl => SecureSocketOptions.SslOnConnect,
            SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, socketOptions, ct);
        if (!string.IsNullOrEmpty(username))
            await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
