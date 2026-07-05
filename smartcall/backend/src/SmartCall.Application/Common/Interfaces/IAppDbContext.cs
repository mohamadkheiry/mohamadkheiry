using Microsoft.EntityFrameworkCore;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Call> Calls { get; }
    DbSet<CallParticipant> CallParticipants { get; }
    DbSet<CallRecording> CallRecordings { get; }
    DbSet<TokenUsageLog> TokenUsageLogs { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<TranslationLanguage> TranslationLanguages { get; }
    DbSet<Font> Fonts { get; }
    DbSet<FontAssignment> FontAssignments { get; }
    DbSet<LandingPageContent> LandingPageContents { get; }
    DbSet<EmailServerSetting> EmailServerSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
