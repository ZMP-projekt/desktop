using Microsoft.EntityFrameworkCore;
using GymAdminPanel.Models;

namespace GymAdminPanel.Data;

public class AppDbContext : DbContext
{
    private readonly string? _databasePath;

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(string databasePath)
    {
        _databasePath = databasePath;
    }

    public DbSet<CacheEntry> CacheEntries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        optionsBuilder.UseSqlite($"Data Source={_databasePath ?? "gym.db"}");
    }
}
