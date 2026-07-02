using NSubstitute;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Application.Features.Calls;
using SmartCall.Domain;
using SmartCall.Domain.Entities;
using Xunit;

namespace SmartCall.Tests;

public class CallHandlerTests
{
    private static ICurrentUserService UserContext(Guid? userId, bool isAdmin = false)
    {
        var svc = Substitute.For<ICurrentUserService>();
        svc.UserId.Returns(userId);
        svc.IsSuperAdmin.Returns(isAdmin);
        return svc;
    }

    private static async Task<(Guid UserId, Call Call)> SeedCallAsync(Infrastructure.Persistence.AppDbContext db)
    {
        var user = new User { Id = Guid.NewGuid(), Email = "host@example.com", PasswordHash = "x", DisplayName = "Host" };
        var call = new Call { Id = Guid.NewGuid(), LinkCode = "test-link-code", CreatedByUserId = user.Id, CreatedBy = user };
        db.Users.Add(user);
        db.Calls.Add(call);
        await db.SaveChangesAsync();
        return (user.Id, call);
    }

    [Fact]
    public async Task CreateCall_requires_login()
    {
        await using var db = TestDb.Create();
        var handler = new CreateCallCommandHandler(db, UserContext(null));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new CreateCallCommand(), default));
    }

    [Fact]
    public async Task CreateCall_generates_unique_link()
    {
        await using var db = TestDb.Create();
        var user = new User { Id = Guid.NewGuid(), Email = "u@example.com", PasswordHash = "x", DisplayName = "U" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new CreateCallCommandHandler(db, UserContext(user.Id));
        var a = await handler.Handle(new CreateCallCommand(), default);
        var b = await handler.Handle(new CreateCallCommand(), default);

        Assert.NotEqual(a.LinkCode, b.LinkCode);
        Assert.True(a.LinkCode.Length >= 12);
    }

    [Fact]
    public async Task JoinCall_guest_joins_without_account()
    {
        await using var db = TestDb.Create();
        var (_, call) = await SeedCallAsync(db);

        var handler = new JoinCallCommandHandler(db, UserContext(null));
        var result = await handler.Handle(new JoinCallCommand(call.LinkCode, "Guest"), default);

        Assert.Equal(ParticipantRole.Guest, result.Role);
    }

    [Fact]
    public async Task JoinCall_rejects_third_regular_participant()
    {
        await using var db = TestDb.Create();
        var (hostId, call) = await SeedCallAsync(db);

        var host = new JoinCallCommandHandler(db, UserContext(hostId));
        await host.Handle(new JoinCallCommand(call.LinkCode, "Host"), default);
        var guest = new JoinCallCommandHandler(db, UserContext(null));
        await guest.Handle(new JoinCallCommand(call.LinkCode, "Guest 1"), default);

        await Assert.ThrowsAsync<ConflictException>(() =>
            guest.Handle(new JoinCallCommand(call.LinkCode, "Guest 2"), default));
    }

    [Fact]
    public async Task JoinCall_superadmin_joins_full_call()
    {
        await using var db = TestDb.Create();
        var (hostId, call) = await SeedCallAsync(db);
        var admin = new User { Id = Guid.NewGuid(), Email = "admin@example.com", PasswordHash = "x", DisplayName = "Admin", IsSuperAdmin = true };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        await new JoinCallCommandHandler(db, UserContext(hostId)).Handle(new JoinCallCommand(call.LinkCode, "Host"), default);
        await new JoinCallCommandHandler(db, UserContext(null)).Handle(new JoinCallCommand(call.LinkCode, "Guest"), default);

        var result = await new JoinCallCommandHandler(db, UserContext(admin.Id, isAdmin: true))
            .Handle(new JoinCallCommand(call.LinkCode, "Admin"), default);

        Assert.Equal(ParticipantRole.SuperAdmin, result.Role);
    }

    [Fact]
    public async Task SetLanguage_rejects_unknown_language()
    {
        await using var db = TestDb.Create();
        var (hostId, call) = await SeedCallAsync(db);
        var join = await new JoinCallCommandHandler(db, UserContext(hostId))
            .Handle(new JoinCallCommand(call.LinkCode, "Host"), default);

        var handler = new SetParticipantLanguageCommandHandler(db);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            handler.Handle(new SetParticipantLanguageCommand(join.ParticipantId, "xx"), default));
    }

    [Fact]
    public async Task SetLanguage_accepts_active_language()
    {
        await using var db = TestDb.Create();
        var (hostId, call) = await SeedCallAsync(db);
        db.TranslationLanguages.Add(new TranslationLanguage
        {
            Id = Guid.NewGuid(), Code = "fa", EnglishName = "Persian", NativeName = "فارسی", IsRtl = true, IsActive = true
        });
        await db.SaveChangesAsync();

        var join = await new JoinCallCommandHandler(db, UserContext(hostId))
            .Handle(new JoinCallCommand(call.LinkCode, "Host"), default);

        await new SetParticipantLanguageCommandHandler(db)
            .Handle(new SetParticipantLanguageCommand(join.ParticipantId, "fa"), default);

        var participant = await db.CallParticipants.FindAsync(join.ParticipantId);
        Assert.Equal("fa", participant!.TargetLanguageCode);
    }
}
