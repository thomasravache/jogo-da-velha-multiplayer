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

    private static readonly int[] TopRow = [0, 1, 2];

    private static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], [0, 3, 6], [1, 4, 7], [2, 5, 8], [0, 4, 8], [2, 4, 6],
    ];

    [Fact(DisplayName = "SPEC-0031:UT-05 — WinningLine é a linha correta nas 8 vitórias e nula nos demais casos")]
    [Trait("Category", "SPEC-0031:UT-05")]
    public void WinningLine_ShouldMatchEachLine_AndBeNullOtherwise()
    {
        foreach (var line in Lines)
        {
            using var game = new GameSession(enableBackgroundTimer: false);
            Assert.Null(game.WinningLine);
            var others = Enumerable.Range(0, 9).Where(i => !line.Contains(i)).Take(2).ToArray();

            game.MakeMove(line[0], Player.X);
            game.MakeMove(others[0], Player.O);
            game.MakeMove(line[1], Player.X);
            game.MakeMove(others[1], Player.O);
            Assert.Null(game.WinningLine);
            game.MakeMove(line[2], Player.X);

            Assert.Equal(Player.X, game.Winner);
            Assert.Equal(line.OrderBy(x => x).ToArray(), game.WinningLine!.ToArray());

            game.Restart();
            Assert.Null(game.WinningLine);
        }

        using var draw = new GameSession(enableBackgroundTimer: false);
        foreach (var (cell, player) in new[] { (0, Player.X), (1, Player.O), (2, Player.X), (4, Player.O), (3, Player.X), (5, Player.O), (7, Player.X), (6, Player.O), (8, Player.X) })
            draw.MakeMove(cell, player);
        Assert.True(draw.IsDraw);
        Assert.Null(draw.WinningLine);

        using var timeout = new GameSession(enableBackgroundTimer: false);
        for (var i = 0; i < GameSession.DefaultTurnTimeSeconds; i++) timeout.Tick();
        Assert.True(timeout.IsTimedOut);
        Assert.Null(timeout.WinningLine);
    }

    [Fact(DisplayName = "SPEC-0031:UT-05b — WinningLine não expõe a tabela interna de linhas (cópia defensiva)")]
    [Trait("Category", "SPEC-0031:UT-05")]
    public void WinningLine_ShouldNotExposeSharedTable()
    {
        using var first = new GameSession(enableBackgroundTimer: false);
        foreach (var (cell, who) in new[] { (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X) })
            first.MakeMove(cell, who);

        Assert.False(first.WinningLine is int[]);

        using var second = new GameSession(enableBackgroundTimer: false);
        foreach (var (cell, who) in new[] { (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X) })
            second.MakeMove(cell, who);
        Assert.Equal(TopRow, second.WinningLine!.ToArray());
    }
}
