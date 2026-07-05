using Microsoft.EntityFrameworkCore;
using SmartCall.Infrastructure.Persistence;

namespace SmartCall.Tests;

public static class TestDb
{
    public static AppDbContext Create()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"smartcall-tests-{Guid.NewGuid()}")
            .Options);
}
