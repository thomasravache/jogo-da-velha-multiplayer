using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Tests;

public class LeaderboardTests
{
    private static GameplayDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<GameplayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GameplayDbContext(options);
    }

    private static GameResultService CreateService(GameplayDbContext ctx) =>
        new(ctx, NullLogger<GameResultService>.Instance);

    [Fact(DisplayName = "SPEC-0017:UT-01 — Agrupa vitórias por jogador corretamente")]
    [Trait("Category", "SPEC-0017:UT-01")]
    public async Task GetLeaderboardAsync_ShouldAggregateWinsCorrectly()
    {
        await using var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        ctx.MatchResults.AddRange(
            new MatchResult { Id = Guid.NewGuid(), PlayerXName = "Thomas", PlayerOName = "Ana", WinnerName = "Thomas", PlayedAt = DateTime.UtcNow },
            new MatchResult { Id = Guid.NewGuid(), PlayerXName = "Thomas", PlayerOName = "Bob", WinnerName = "Thomas", PlayedAt = DateTime.UtcNow },
            new MatchResult { Id = Guid.NewGuid(), PlayerXName = "Ana", PlayerOName = "Bob", WinnerName = "Ana", PlayedAt = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();

        var leaderboard = await service.GetLeaderboardAsync(10);

        Assert.Equal(2, leaderboard.Count);
        Assert.Equal("Thomas", leaderboard[0].PlayerName);
        Assert.Equal(2, leaderboard[0].Wins);
        Assert.Equal("Ana", leaderboard[1].PlayerName);
        Assert.Equal(1, leaderboard[1].Wins);
    }

    [Fact(DisplayName = "SPEC-0017:UT-02 — Ignora empates na contagem de vitórias")]
    [Trait("Category", "SPEC-0017:UT-02")]
    public async Task GetLeaderboardAsync_ShouldIgnoreDraws()
    {
        await using var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        ctx.MatchResults.AddRange(
            new MatchResult { Id = Guid.NewGuid(), PlayerXName = "Thomas", PlayerOName = "Ana", WinnerName = null, PlayedAt = DateTime.UtcNow },
            new MatchResult { Id = Guid.NewGuid(), PlayerXName = "Thomas", PlayerOName = "Bob", WinnerName = null, PlayedAt = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();

        var leaderboard = await service.GetLeaderboardAsync(10);

        Assert.Empty(leaderboard);
    }

    [Fact(DisplayName = "SPEC-0017:IT-01 — Ordena por vitórias decrescentes e respeita o limite")]
    [Trait("Category", "SPEC-0017:IT-01")]
    public async Task GetLeaderboardAsync_ShouldSortAndLimitResults()
    {
        await using var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        for (int i = 1; i <= 15; i++)
        {
            string player = $"Player{i}";
            for (int w = 0; w < i; w++)
            {
                ctx.MatchResults.Add(new MatchResult
                {
                    Id = Guid.NewGuid(),
                    PlayerXName = player,
                    PlayerOName = "Other",
                    WinnerName = player,
                    PlayedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }
        }
        await ctx.SaveChangesAsync();

        var top5 = await service.GetLeaderboardAsync(5);

        Assert.Equal(5, top5.Count);
        Assert.Equal("Player15", top5[0].PlayerName);
        Assert.Equal(15, top5[0].Wins);
        Assert.Equal("Player14", top5[1].PlayerName);
        Assert.Equal(14, top5[1].Wins);
    }
}
