using Microsoft.EntityFrameworkCore;
using GymAdminPanel.Models;

namespace GymAdminPanel.Data;

public class AppDbContext : DbContext
{
    public DbSet<Client> Clients { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=gym.db");
    }
}