using System;
using System.Linq;
using Xunit;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Tests;

public class AiPlayerTests
{
    [Fact(DisplayName = "SPEC-0015:UT-01 — Modo Fácil retorna índice válido e livre")]
    [Trait("Category", "SPEC-0015:UT-01")]
    public void EasyMode_ShouldReturnValidAndEmptyIndex()
    {
        var game = new GameSession();
        game.MakeMove(0, Player.X);

        int move = AiPlayer.GetBestMove(game, Player.O, AiDifficulty.Easy);

        Assert.InRange(move, 0, 8);
        Assert.NotEqual(0, move);
        Assert.Equal(Player.None, game.Board[move]);
    }

    [Fact(DisplayName = "SPEC-0015:UT-02 — Modo Difícil bloqueia vitória imediata do oponente")]
    [Trait("Category", "SPEC-0015:UT-02")]
    public void HardMode_ShouldBlockOpponentImmediateWin()
    {
        var game = new GameSession();
        // X joga 0 e 1 (ameaça vitória no índice 2)
        game.MakeMove(0, Player.X);
        game.MakeMove(4, Player.O); // centro
        game.MakeMove(1, Player.X);

        int aiMove = AiPlayer.GetBestMove(game, Player.O, AiDifficulty.Hard);

        // O deve bloquear jogando em 2
        Assert.Equal(2, aiMove);
    }

    [Fact(DisplayName = "SPEC-0015:UT-03 — Modo Difícil escolhe a vitória imediata")]
    [Trait("Category", "SPEC-0015:UT-03")]
    public void HardMode_ShouldTakeImmediateWin()
    {
        var game = new GameSession();
        // O tem 3 e 4, pode vencer em 5
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(8, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(1, Player.X);

        int aiMove = AiPlayer.GetBestMove(game, Player.O, AiDifficulty.Hard);

        // O deve vencer em 5
        Assert.Equal(5, aiMove);
    }

    [Fact(DisplayName = "SPEC-0015:IT-01 — Minimax nunca é derrotado em simulações")]
    [Trait("Category", "SPEC-0015:IT-01")]
    public void HardMode_ShouldNeverLoseAgainstVariousMoves()
    {
        // Testar múltiplos movimentos iniciais do humano
        for (int initialHumanMove = 0; initialHumanMove < 9; initialHumanMove++)
        {
            var game = new GameSession();
            game.MakeMove(initialHumanMove, Player.X);

            while (game.Winner == Player.None && !game.IsDraw)
            {
                if (game.CurrentTurn == Player.O)
                {
                    int aiMove = AiPlayer.GetBestMove(game, Player.O, AiDifficulty.Hard);
                    Assert.InRange(aiMove, 0, 8);
                    Assert.Equal(Player.None, game.Board[aiMove]);
                    game.MakeMove(aiMove, Player.O);
                }
                else
                {
                    // Humano pega primeiro espaço livre
                    int firstFree = Array.IndexOf(game.Board, Player.None);
                    if (firstFree == -1) break;
                    game.MakeMove(firstFree, Player.X);
                }
            }

            // O bot (Player.O) JAMAIS pode perder
            Assert.NotEqual(Player.X, game.Winner);
        }
    }
}
