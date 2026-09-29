using Xunit;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Tests;

public class ScoreboardTests
{
    [Fact(DisplayName = "SPEC-0014:UT-01 — GameSession inicia com placar zero para X e O")]
    [Trait("Category", "SPEC-0014:UT-01")]
    public void NewGameSession_ShouldHaveZeroScores()
    {
        var game = new GameSession();
        Assert.Equal(0, game.GetScore(Player.X));
        Assert.Equal(0, game.GetScore(Player.O));
    }

    [Fact(DisplayName = "SPEC-0014:UT-02 — Vitória incrementa placar do vencedor")]
    [Trait("Category", "SPEC-0014:UT-02")]
    public void WinningMove_ShouldIncrementWinnerScore()
    {
        var game = new GameSession();
        // X vence na primeira linha
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X);

        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(1, game.GetScore(Player.X));
        Assert.Equal(0, game.GetScore(Player.O));
    }

    [Fact(DisplayName = "SPEC-0014:UT-03 — Empate não altera placares")]
    [Trait("Category", "SPEC-0014:UT-03")]
    public void Draw_ShouldNotIncrementScores()
    {
        var game = new GameSession();
        // Sequência de empate
        game.MakeMove(0, Player.X);
        game.MakeMove(1, Player.O);
        game.MakeMove(2, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(5, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(6, Player.X);
        game.MakeMove(8, Player.O);
        game.MakeMove(7, Player.X);

        Assert.True(game.IsDraw);
        Assert.Equal(0, game.GetScore(Player.X));
        Assert.Equal(0, game.GetScore(Player.O));
    }

    [Fact(DisplayName = "SPEC-0014:IT-01 — Restart preserva placares acumulados")]
    [Trait("Category", "SPEC-0014:IT-01")]
    public void Restart_ShouldPreserveAccumulatedScores()
    {
        var game = new GameSession();
        // Rodada 1: X vence
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X);

        Assert.Equal(1, game.GetScore(Player.X));

        // Rematch
        game.Restart();
        Assert.Equal(1, game.GetScore(Player.X));
        Assert.Equal(0, game.GetScore(Player.O));

        // Rodada 2: O vence
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(8, Player.X);
        game.MakeMove(5, Player.O); // O vence linha do meio (3, 4, 5)

        Assert.Equal(Player.O, game.Winner);
        Assert.Equal(1, game.GetScore(Player.X));
        Assert.Equal(1, game.GetScore(Player.O));
    }
}
