using System;
using Xunit;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;

namespace TicTacToe.Tests;

// SPEC-0009: Player Names feature tests

public class PlayerNamesGameplayTests
{
    [Fact(DisplayName = "SPEC-0009:UT-01 — GameSession retorna fallback 'X' quando nome não definido")]
    [Trait("Category", "SPEC-0009:UT-01")]
    public void GetPlayerName_ShouldReturnFallback_WhenNameNotSet()
    {
        // Arrange
        var game = new GameSession();

        // Act
        var name = game.GetPlayerName(Player.X);

        // Assert
        Assert.Equal("X", name);
    }

    [Fact(DisplayName = "SPEC-0009:UT-02 — GameSession retorna nome correto quando definido")]
    [Trait("Category", "SPEC-0009:UT-02")]
    public void GetPlayerName_ShouldReturnName_WhenNameIsSet()
    {
        // Arrange
        var game = new GameSession();
        game.SetPlayerName(Player.X, "Thomas");

        // Act
        var name = game.GetPlayerName(Player.X);

        // Assert
        Assert.Equal("Thomas", name);
    }

    [Fact(DisplayName = "SPEC-0009:UT-04 — IsDraw não regride com nomes configurados")]
    [Trait("Category", "SPEC-0009:UT-04")]
    public void IsDraw_ShouldStillWork_WhenNamesAreSet()
    {
        // Arrange
        var game = new GameSession();
        game.SetPlayerName(Player.X, "Thomas");
        game.SetPlayerName(Player.O, "Ana");

        // Fill board with no winner: X O X / O X O / O X O
        game.MakeMove(0, Player.X); // X
        game.MakeMove(1, Player.O); // O
        game.MakeMove(2, Player.X); // X
        game.MakeMove(3, Player.O); // O
        game.MakeMove(4, Player.X); // X — win candidate but not row/col/diag complete
        game.MakeMove(5, Player.O); // O
        game.MakeMove(7, Player.X); // X
        game.MakeMove(6, Player.O); // O
        game.MakeMove(8, Player.X); // X — but X wins 0,4,8 diag after this! Let's use a draw pattern instead.

        // Reset: use a known draw sequence
        var draw = new GameSession();
        draw.SetPlayerName(Player.X, "Thomas");
        draw.SetPlayerName(Player.O, "Ana");
        // X O X / O O X / X X O — Draw
        draw.MakeMove(0, Player.X);
        draw.MakeMove(1, Player.O);
        draw.MakeMove(2, Player.X);
        draw.MakeMove(3, Player.O);
        draw.MakeMove(5, Player.X);
        draw.MakeMove(4, Player.O);
        draw.MakeMove(6, Player.X);
        draw.MakeMove(8, Player.O);
        draw.MakeMove(7, Player.X);

        // Assert
        Assert.True(draw.IsDraw);
        Assert.Equal(Player.None, draw.Winner);
    }
}

public class PlayerNamesMatchmakingTests
{
    [Fact(DisplayName = "SPEC-0009:UT-03 — MatchmakingService armazena e recupera nomes")]
    [Trait("Category", "SPEC-0009:UT-03")]
    public void JoinQueue_ShouldStoreAndReturnPlayerNames()
    {
        // Arrange
        var service = new MatchmakingService();
        var conn1 = Guid.NewGuid().ToString();
        var conn2 = Guid.NewGuid().ToString();

        // Act
        service.JoinQueue(conn1, "Thomas");
        service.JoinQueue(conn2, "Ana");

        // Assert
        Assert.Equal("Thomas", service.GetPlayerName(conn1));
        Assert.Equal("Ana", service.GetPlayerName(conn2));
    }

    [Fact(DisplayName = "SPEC-0009:IT-01 — Nomes chegam corretamente no GameSession após matchmaking")]
    [Trait("Category", "SPEC-0009:IT-01")]
    public void JoinQueue_ShouldProduceMatch_ThenNamesCanBeAppliedToGameSession()
    {
        // Arrange
        var service = new MatchmakingService();
        var conn1 = Guid.NewGuid().ToString();
        var conn2 = Guid.NewGuid().ToString();

        // Act
        var noMatch = service.JoinQueue(conn1, "Thomas");
        var matchId = service.JoinQueue(conn2, "Ana");

        // Assert - match was made
        Assert.Null(noMatch);
        Assert.NotNull(matchId);

        // Simulate what the Blazor component does: create GameSession and set names
        var game = new GameSession();
        // conn1 is Player.X (first in queue), conn2 is Player.O (second)
        game.SetPlayerName(Player.X, service.GetPlayerName(conn1)!);
        game.SetPlayerName(Player.O, service.GetPlayerName(conn2)!);

        Assert.Equal("Thomas", game.GetPlayerName(Player.X));
        Assert.Equal("Ana", game.GetPlayerName(Player.O));
    }
}
