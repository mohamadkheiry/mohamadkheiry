using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Admin;

public record SmtpSettingsDto(string? Host, int Port, string? Username, bool HasPassword,
    SmtpSecurityMode SecurityMode, string? SenderName, string? SenderEmail);

public record GetSmtpSettingsQuery : IRequest<SmtpSettingsDto>;

public class GetSmtpSettingsQueryHandler(IAppDbContext db) : IRequestHandler<GetSmtpSettingsQuery, SmtpSettingsDto>
{
    public async Task<SmtpSettingsDto> Handle(GetSmtpSettingsQuery request, CancellationToken ct)
    {
        var s = await db.EmailServerSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return s is null
            ? new SmtpSettingsDto(null, 587, null, false, SmtpSecurityMode.StartTls, null, null)
            : new SmtpSettingsDto(s.Host, s.Port, s.Username, !string.IsNullOrEmpty(s.Password), s.SecurityMode, s.SenderName, s.SenderEmail);
    }
}

public record UpdateSmtpSettingsCommand(string Host, int Port, string Username, string? Password,
    SmtpSecurityMode SecurityMode, string SenderName, string SenderEmail) : IRequest;

public class UpdateSmtpSettingsCommandValidator : AbstractValidator<UpdateSmtpSettingsCommand>
{
    public UpdateSmtpSettingsCommandValidator()
    {
        RuleFor(x => x.Host).NotEmpty();
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.SenderEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.SenderName).NotEmpty();
    }
}

public class UpdateSmtpSettingsCommandHandler(IAppDbContext db, IEncryptionService encryption)
    : IRequestHandler<UpdateSmtpSettingsCommand>
{
    public async Task Handle(UpdateSmtpSettingsCommand request, CancellationToken ct)
    {
        var s = await db.EmailServerSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new EmailServerSetting { Id = Guid.NewGuid(), Password = "" };
            db.EmailServerSettings.Add(s);
        }

        s.Host = request.Host.Trim();
        s.Port = request.Port;
        s.Username = request.Username.Trim();
        if (!string.IsNullOrEmpty(request.Password))
            s.Password = encryption.Encrypt(request.Password);
        s.SecurityMode = request.SecurityMode;
        s.SenderName = request.SenderName.Trim();
        s.SenderEmail = request.SenderEmail.Trim();
        s.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}

public record SendTestEmailCommand(string ToEmail) : IRequest;

public class SendTestEmailCommandHandler(IEmailService email) : IRequestHandler<SendTestEmailCommand>
{
    public Task Handle(SendTestEmailCommand request, CancellationToken ct)
        => email.SendAsync(request.ToEmail, "SmartCall — ایمیل آزمایشی / Test email",
            "<p>تنظیمات سرور ایمیل SmartCall به‌درستی کار می‌کند.</p><p>Your SmartCall SMTP settings are working correctly.</p>", ct);
}
