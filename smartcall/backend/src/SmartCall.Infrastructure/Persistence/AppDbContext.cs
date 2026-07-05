using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Call> Calls => Set<Call>();
    public DbSet<CallParticipant> CallParticipants => Set<CallParticipant>();
    public DbSet<CallRecording> CallRecordings => Set<CallRecording>();
    public DbSet<TokenUsageLog> TokenUsageLogs => Set<TokenUsageLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<TranslationLanguage> TranslationLanguages => Set<TranslationLanguage>();
    public DbSet<Font> Fonts => Set<Font>();
    public DbSet<FontAssignment> FontAssignments => Set<FontAssignment>();
    public DbSet<LandingPageContent> LandingPageContents => Set<LandingPageContent>();
    public DbSet<EmailServerSetting> EmailServerSettings => Set<EmailServerSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(320);
            e.Property(u => u.DisplayName).HasMaxLength(100);
        });

        builder.Entity<Call>(e =>
        {
            e.HasIndex(c => c.LinkCode).IsUnique();
            e.Property(c => c.LinkCode).HasMaxLength(32);
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CallParticipant>(e =>
        {
            e.HasOne(p => p.Call).WithMany(c => c.Participants).HasForeignKey(p => p.CallId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.User).WithMany(u => u.Participations).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
            e.Property(p => p.TargetLanguageCode).HasMaxLength(10);
            e.Property(p => p.DisplayName).HasMaxLength(100);
        });

        builder.Entity<CallRecording>(e =>
        {
            e.HasOne(r => r.Call).WithMany(c => c.Recordings).HasForeignKey(r => r.CallId).OnDelete(DeleteBehavior.Cascade);
            e.Property(r => r.FilePath).HasMaxLength(500);
        });

        builder.Entity<TokenUsageLog>(e =>
        {
            e.HasOne(l => l.User).WithMany(u => u.TokenUsages).HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(l => l.Call).WithMany(c => c.TokenUsages).HasForeignKey(l => l.CallId).OnDelete(DeleteBehavior.SetNull);
            e.Property(l => l.Model).HasMaxLength(100);
            e.HasIndex(l => l.CreatedAt);
        });

        builder.Entity<AppSetting>(e =>
        {
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasMaxLength(100);
        });

        builder.Entity<TranslationLanguage>(e =>
        {
            e.HasIndex(l => l.Code).IsUnique();
            e.Property(l => l.Code).HasMaxLength(10);
        });

        builder.Entity<FontAssignment>(e =>
        {
            e.HasIndex(a => new { a.Scope, a.Language }).IsUnique();
            e.Property(a => a.Language).HasMaxLength(5);
            e.HasOne(a => a.Font).WithMany().HasForeignKey(a => a.FontId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LandingPageContent>(e =>
        {
            e.HasIndex(c => new { c.SectionKey, c.Language }).IsUnique();
            e.Property(c => c.SectionKey).HasMaxLength(100);
            e.Property(c => c.Language).HasMaxLength(5);
        });
    }
}
