using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartCall.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef` at design time (creating/scripting migrations).
/// The connection string comes from SMARTCALL_DB env var when set; migrations
/// don't need a live database to be generated.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SMARTCALL_DB")
            ?? "Host=localhost;Port=5432;Database=smartcall;Username=smartcall;Password=smartcall";
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options);
    }
}
