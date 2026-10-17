using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ChessCore.Data;

// Schema tooling never runs application startup/SMTP/seed logic or auto-detects a server.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables().Build();
        var connection = config.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=ChessCore;User=root;Password=;";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 30))).Options;
        return new AppDbContext(options);
    }
}
