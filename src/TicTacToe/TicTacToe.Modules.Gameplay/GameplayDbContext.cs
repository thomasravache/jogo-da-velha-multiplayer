using Microsoft.EntityFrameworkCore;

namespace TicTacToe.Modules.Gameplay;

public class GameplayDbContext(DbContextOptions<GameplayDbContext> options) : DbContext(options)
{
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Gameplay");

        modelBuilder.Entity<MatchResult>(e =>
        {
            e.ToTable("MatchResults");
            e.HasKey(m => m.Id);
            e.Property(m => m.PlayerXName).HasMaxLength(20).IsRequired();
            e.Property(m => m.PlayerOName).HasMaxLength(20).IsRequired();
            e.Property(m => m.WinnerName).HasMaxLength(20);
            e.Property(m => m.PlayedAt).IsRequired();
        });
    }
}
