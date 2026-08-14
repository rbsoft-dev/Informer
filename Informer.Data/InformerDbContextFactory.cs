using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Informer.Data;

/// <summary>
/// »спользуетс€ только инструментами EF Core ("Add-Migration" / "dotnet ef migrations add")
/// дл€ создани€ экземпл€ра DbContext во врем€ разработки (design time), когда реальный
/// хост (Informer.App) не запущен. ‘актический провайдер/строка подключени€ времени
/// выполнени€ настраиваетс€ в Informer.App/Program.cs и может отличатьс€ (например,
/// указывать на другой файл SQLite или вообще на другого провайдера).
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
