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
            e.Property(m => m.DurationSeconds);
            e.Property(m => m.MoveCount);
            e.Property(m => m.EndReason).HasConversion<string>().HasMaxLength(16);
            e.Property(m => m.WinnerSide).HasMaxLength(1);
            e.Property(m => m.WinningLine).HasMaxLength(5);
            e.Property(m => m.FinalBoard).HasMaxLength(9);
            e.Property(m => m.Mode).HasConversion<string>().HasMaxLength(8);
            e.HasIndex(m => m.PlayerXId);
            e.HasIndex(m => m.PlayerOId);
        });
    }
}
