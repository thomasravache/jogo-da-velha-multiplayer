using System;
using Xunit;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;

namespace TicTacToe.Tests;

public class GameplayTests
{
    [Fact]
    [Trait("Category", "SPEC-0007:UT-01")]
    public void MakeMove_ShouldToggleTurnsAndDetectWinner()
    {
        // Arrange
        var game = new GameSession();

        // Act & Assert
        Assert.Equal(Player.X, game.CurrentTurn);

        Assert.True(game.MakeMove(0, Player.X)); // X top-left
        Assert.Equal(Player.O, game.CurrentTurn);

        Assert.True(game.MakeMove(3, Player.O)); // O mid-left
        Assert.Equal(Player.X, game.CurrentTurn);

        Assert.True(game.MakeMove(1, Player.X)); // X top-mid
        Assert.True(game.MakeMove(4, Player.O)); // O mid-mid

        Assert.True(game.MakeMove(2, Player.X)); // X top-right, WIN!

        // Assert
        Assert.Equal(Player.X, game.Winner);
        Assert.False(game.MakeMove(5, Player.O)); // O cannot move after game ends
    }

    [Fact(DisplayName = "SPEC-0012:UT-01 — Restart limpa tabuleiro e winner, mantendo nomes dos jogadores")]
    [Trait("Category", "SPEC-0012:UT-01")]
    public void Restart_ShouldResetBoardAndWinner_AndPreservePlayerNames()
    {
        // Arrange
        var game = new GameSession();
        game.SetPlayerName(Player.X, "Thomas");
        game.SetPlayerName(Player.O, "Ana");

        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X); // X wins

        Assert.Equal(Player.X, game.Winner);

        // Act
        game.Restart();

        // Assert
        Assert.Equal(Player.None, game.Winner);
        Assert.False(game.IsDraw);
        Assert.Equal(Player.X, game.CurrentTurn);
        Assert.All(game.Board, cell => Assert.Equal(Player.None, cell));
        Assert.Equal("Thomas", game.GetPlayerName(Player.X));
        Assert.Equal("Ana", game.GetPlayerName(Player.O));
    }
}

public class MatchmakingTests
{
    [Fact]
    [Trait("Category", "SPEC-0006:UT-01")]
    public void JoinQueue_ShouldPairTwoPlayers()
    {
        // Arrange
        var service = new MatchmakingService();
        var player1 = Guid.NewGuid().ToString();
        var player2 = Guid.NewGuid().ToString();
        var player3 = Guid.NewGuid().ToString();

        // Act
        var match1 = service.JoinQueue(player1);
        var match2 = service.JoinQueue(player2);
        var match3 = service.JoinQueue(player3);

        // Assert
        Assert.Null(match1); // Player 1 waits
        Assert.NotNull(match2); // Player 2 joins and forms a match

        // Verify they are in the same match
        Assert.True(service.ActiveMatches.TryGetValue(player1, out var p1Match));
        Assert.True(service.ActiveMatches.TryGetValue(player2, out var p2Match));
        Assert.Equal(p1Match, p2Match);

        // Player 3 should be waiting
        Assert.Null(match3);
    }
}
