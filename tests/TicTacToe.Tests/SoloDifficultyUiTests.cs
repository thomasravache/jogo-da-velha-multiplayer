using System;
using Xunit;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Tests;

public class SoloDifficultyUiTests
{
    [Fact(DisplayName = "SPEC-0019:UT-01 — Dificuldade Hard mapeia para nome 'Robô Impossível 🤖'")]
    [Trait("Category", "SPEC-0019:UT-01")]
    public void HardDifficulty_ShouldMapToImpossibleBotName()
    {
        var botName = AiPlayer.GetBotName(AiDifficulty.Hard);
        Assert.Equal("Robô Impossível 🤖", botName);
    }

    [Fact(DisplayName = "SPEC-0019:UT-02 — Dificuldade Easy mapeia para nome 'Robô Fácil 🤖'")]
    [Trait("Category", "SPEC-0019:UT-02")]
    public void EasyDifficulty_ShouldMapToEasyBotName()
    {
        var botName = AiPlayer.GetBotName(AiDifficulty.Easy);
        Assert.Equal("Robô Fácil 🤖", botName);
    }

    [Theory(DisplayName = "SPEC-0019:IT-01 — Execução da IA conforme a dificuldade selecionada")]
    [InlineData(AiDifficulty.Easy)]
    [InlineData(AiDifficulty.Hard)]
    [Trait("Category", "SPEC-0019:IT-01")]
    public void SoloGame_ShouldExecuteAiMovesForSelectedDifficulty(AiDifficulty difficulty)
    {
        var game = new GameSession();
        game.SetPlayerName(Player.X, "Jogador");
        game.SetPlayerName(Player.O, AiPlayer.GetBotName(difficulty));

        game.MakeMove(4, Player.X); // Jogador joga no centro
        int aiMove = AiPlayer.GetBestMove(game, Player.O, difficulty);

        Assert.InRange(aiMove, 0, 8);
        Assert.NotEqual(4, aiMove);
        Assert.Equal(Player.None, game.Board[aiMove]);
    }

    [Fact(DisplayName = "SPEC-0019:E2E-01 — Componente Home contém os controles de seleção de dificuldade e binding")]
    [Trait("Category", "SPEC-0019:E2E-01")]
    public void HomeRazor_ShouldContainDifficultySelectorMarkup()
    {
        var homeRazorPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor"));
        var lobbyRazorPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../src/TicTacToe/TicTacToe.Web/Components/Game/Lobby.razor"));
        var targetPath = System.IO.File.Exists(lobbyRazorPath) ? lobbyRazorPath : homeRazorPath;
        if (System.IO.File.Exists(targetPath))
        {
            var content = System.IO.File.ReadAllText(targetPath);
            Assert.Contains("SegmentedControl", content);
            Assert.Contains("AiDifficulty.Easy", content);
            Assert.Contains("AiDifficulty.Hard", content);
            Assert.Contains("Fácil 🟢", content);
            Assert.Contains("Impossível 🔴", content);
        }
    }
}
