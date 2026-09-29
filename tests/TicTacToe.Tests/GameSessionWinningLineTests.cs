using System;
using System.Linq;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0031: linha vencedora do GameSession

public class GameSessionWinningLineTests
{
    [Fact(DisplayName = "SPEC-0031:CH-01 — Regras de jogada, vitória e reinício permanecem as mesmas")]
    [Trait("Category", "SPEC-0031:CH-01")]
    public void MakeMove_ShouldKeepTurnWinAndRestartRules()
    {
        using var game = new GameSession(enableBackgroundTimer: false);

        Assert.True(game.MakeMove(4, Player.X));
        Assert.False(game.MakeMove(4, Player.O));
        Assert.False(game.MakeMove(0, Player.X));
        Assert.True(game.MakeMove(0, Player.O));
        Assert.True(game.MakeMove(1, Player.X));
        Assert.True(game.MakeMove(2, Player.O));
        Assert.True(game.MakeMove(7, Player.X));

        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(1, game.GetScore(Player.X));

        game.Restart();
        Assert.Equal(Player.None, game.Winner);
        Assert.Equal(Player.X, game.CurrentTurn);
        Assert.All(game.Board, p => Assert.Equal(Player.None, p));
    }
}
