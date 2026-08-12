using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Informer.Data;

/// <summary>
/// Used only by EF Core tooling ("Add-Migration" / "dotnet ef migrations add") to create
/// a DbContext instance at design time, when the real host (Informer.App) isn't running.
/// The actual runtime provider/connection string is configured in Informer.App/Program.cs
/// and can differ (e.g. point at a different SQLite file, or a different provider entirely).
/// </summary>
public class InformerDbContextFactory : IDesignTimeDbContextFactory<InformerDbContext>
{
    public InformerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InformerDbContext>();
        optionsBuilder.UseSqlite("Data Source=informer.db");
        return new InformerDbContext(optionsBuilder.Options);
    }
}
