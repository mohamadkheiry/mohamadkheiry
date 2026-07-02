using NSubstitute;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Application.Features.Auth;
using SmartCall.Domain.Entities;
using Xunit;

namespace SmartCall.Tests;

public class AuthHandlerTests
{
    private static (IPasswordHasher Hasher, IJwtTokenService Jwt) CreateMocks()
    {
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash(Arg.Any<string>()).Returns(ci => "hashed:" + ci.Arg<string>());
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => "hashed:" + ci.ArgAt<string>(0) == ci.ArgAt<string>(1));

        var jwt = Substitute.For<IJwtTokenService>();
        jwt.CreateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<bool>()).Returns("test-token");
        return (hasher, jwt);
    }

    [Fact]
    public async Task Register_creates_user_and_returns_token()
    {
        await using var db = TestDb.Create();
        var (hasher, jwt) = CreateMocks();
        var handler = new RegisterCommandHandler(db, hasher, jwt);

        var result = await handler.Handle(new RegisterCommand("User@Example.com", "password123", "Ali"), default);

        Assert.Equal("test-token", result.Token);
        Assert.Equal("user@example.com", result.Email);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        await using var db = TestDb.Create();
        var (hasher, jwt) = CreateMocks();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "x", DisplayName = "Existing" });
        await db.SaveChangesAsync();

        var handler = new RegisterCommandHandler(db, hasher, jwt);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RegisterCommand("user@example.com", "password123", "Ali"), default));
    }

    [Fact]
    public async Task Login_fails_with_wrong_password()
    {
        await using var db = TestDb.Create();
        var (hasher, jwt) = CreateMocks();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "hashed:correct", DisplayName = "Ali" });
        await db.SaveChangesAsync();

        var handler = new LoginCommandHandler(db, hasher, jwt);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new LoginCommand("user@example.com", "wrong"), default));
    }

    [Fact]
    public async Task Login_succeeds_with_correct_password()
    {
        await using var db = TestDb.Create();
        var (hasher, jwt) = CreateMocks();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "hashed:correct", DisplayName = "Ali" });
        await db.SaveChangesAsync();

        var handler = new LoginCommandHandler(db, hasher, jwt);
        var result = await handler.Handle(new LoginCommand("user@example.com", "correct"), default);

        Assert.Equal("test-token", result.Token);
    }
}
