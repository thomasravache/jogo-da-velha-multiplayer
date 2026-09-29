using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Tests;

// SPEC-0010: Histórico de Partidas

public class GameResultServiceTests
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

    [Fact(DisplayName = "SPEC-0010:UT-01 — SaveResultAsync grava vitória com WinnerName correto")]
    [Trait("Category", "SPEC-0010:UT-01")]
    public async Task SaveResultAsync_ShouldSaveWinner_WhenGameHasWinner()
    {
        // Arrange
        await using var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        var game = new GameSession();
        game.SetPlayerName(Player.X, "Thomas");
        game.SetPlayerName(Player.O, "Ana");
        // X wins top row
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X); // X wins

        // Act
        await service.SaveResultAsync(game);

        // Assert
        var result = await ctx.MatchResults.SingleAsync();
        Assert.Equal("Thomas", result.PlayerXName);
        Assert.Equal("Ana", result.PlayerOName);
        Assert.Equal("Thomas", result.WinnerName);
    }

    [Fact(DisplayName = "SPEC-0010:UT-02 — SaveResultAsync grava empate com WinnerName null")]
    [Trait("Category", "SPEC-0010:UT-02")]
    public async Task SaveResultAsync_ShouldSaveNullWinner_WhenGameIsDraw()
    {
        // Arrange
        await using var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        var game = new GameSession();
        game.SetPlayerName(Player.X, "Thomas");
        game.SetPlayerName(Player.O, "Ana");
        // X O X / O O X / X X O — Draw
        game.MakeMove(0, Player.X);
        game.MakeMove(1, Player.O);
        game.MakeMove(2, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(5, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(6, Player.X);
        game.MakeMove(8, Player.O);
        game.MakeMove(7, Player.X);

        // Act
        await service.SaveResultAsync(game);

        // Assert
        var result = await ctx.MatchResults.SingleAsync();
        Assert.Null(result.WinnerName);
        Assert.True(result.PlayedAt <= DateTime.UtcNow);
    }

    [Fact(DisplayName = "SPEC-0010:UT-03 — GetRecentAsync retorna máximo 10 itens ordenados do mais recente")]
    [Trait("Category", "SPEC-0010:UT-03")]
    public async Task GetRecentAsync_ShouldReturnTop10OrderedByDateDesc()
    {
        // Arrange
        await using var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        for (int i = 0; i < 15; i++)
        {
            ctx.MatchResults.Add(new MatchResult
            {
                Id = Guid.NewGuid(),
                PlayerXName = $"X{i}",
                PlayerOName = $"O{i}",
                WinnerName = null,
                PlayedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await ctx.SaveChangesAsync();

        // Act
        var results = await service.GetRecentAsync(10);

        // Assert
        Assert.Equal(10, results.Count);
        Assert.True(results[0].PlayedAt >= results[1].PlayedAt); // ordered DESC
    }

    [Fact(DisplayName = "SPEC-0010:IT-01 — SaveResultAsync persiste no InMemory DbContext")]
    [Trait("Category", "SPEC-0010:IT-01")]
    public async Task SaveResultAsync_ShouldPersistToDatabase()
    {
        // Arrange
        await using var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        var game = new GameSession();
        game.SetPlayerName(Player.X, "Alice");
        game.SetPlayerName(Player.O, "Bob");
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X); // X wins

        // Act
        await service.SaveResultAsync(game);

        // Assert
        Assert.Equal(1, await ctx.MatchResults.CountAsync());
    }

    [Fact(DisplayName = "SPEC-0010:IT-02 — GetRecentAsync retorna resultados em ordem DESC de PlayedAt")]
    [Trait("Category", "SPEC-0010:IT-02")]
    public async Task GetRecentAsync_ShouldReturnResultsInDescendingOrder()
    {
        // Arrange
        await using var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        ctx.MatchResults.AddRange(
            new MatchResult { Id = Guid.NewGuid(), PlayerXName = "A", PlayerOName = "B", PlayedAt = DateTime.UtcNow.AddHours(-3) },
            new MatchResult { Id = Guid.NewGuid(), PlayerXName = "C", PlayerOName = "D", PlayedAt = DateTime.UtcNow.AddHours(-1) },
            new MatchResult { Id = Guid.NewGuid(), PlayerXName = "E", PlayerOName = "F", PlayedAt = DateTime.UtcNow.AddHours(-2) }
        );
        await ctx.SaveChangesAsync();

        // Act
        var results = await service.GetRecentAsync(10);

        // Assert
        Assert.Equal("C", results[0].PlayerXName); // most recent first
        Assert.Equal("E", results[1].PlayerXName);
        Assert.Equal("A", results[2].PlayerXName);
    }
}
