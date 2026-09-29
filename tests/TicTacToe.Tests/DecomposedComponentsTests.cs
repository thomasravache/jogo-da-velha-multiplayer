using System;
using System.IO;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Game;
using Xunit;

namespace TicTacToe.Tests;

public class DecomposedComponentsTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact(DisplayName = "SPEC-0024:UT-01 — Lobby exibe inputs, botões de modo e seletores")]
    [Trait("Category", "SPEC-0024:UT-01")]
    public async Task Lobby_ShouldRenderInputsAndButtons()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        var cut = ctx.Render<Lobby>(parameters => parameters
            .Add(p => p.PlayerName, "Thomas")
            .Add(p => p.SelectedDifficulty, AiDifficulty.Hard));

        // Deve conter campo de apelido e botões
        var markup = cut.Markup;
        Assert.Contains("Jogar Online", markup);
        Assert.Contains("Jogar vs Robô (IA)", markup);
        Assert.Contains("Criar Sala Privada", markup);
        Assert.Contains("Dificuldade:", markup);
    }

    [Fact(DisplayName = "SPEC-0024:UT-02 — Scoreboard renderiza tags e placar dos jogadores")]
    [Trait("Category", "SPEC-0024:UT-02")]
    public void Scoreboard_ShouldRenderPlayerTagsAndScore()
    {
        var game = new GameSession();
        game.SetPlayerName(Player.X, "Alice");
        game.SetPlayerName(Player.O, "Bob");

        using var ctx = new BunitContext();
        var cut = ctx.Render<Scoreboard>(parameters => parameters
            .Add(p => p.Game, game)
            .Add(p => p.MyPlayer, Player.X));

        var markup = cut.Markup;
        Assert.Contains("Alice", markup);
        Assert.Contains("Bob", markup);
        Assert.Contains("players-bar", markup);
        Assert.Contains("status", markup);
    }

    [Fact(DisplayName = "SPEC-0024:IT-01 — GameBoard emite OnCellClick ao clicar em célula jogável")]
    [Trait("Category", "SPEC-0024:IT-01")]
    public async Task GameBoard_ShouldTriggerOnCellClick_WhenClicked()
    {
        using var game = new GameSession();
        int clickedIndex = -1;

        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        var cut = ctx.Render<GameBoard>(parameters => parameters
            .Add(p => p.Game, game)
            .Add(p => p.MyPlayer, Player.X)
            .Add(p => p.OnCellClick, (int idx) => clickedIndex = idx));

        var cell = cut.Find(".cell.playable");
        cell.Click();

        Assert.True(clickedIndex >= 0, "Deveria ter disparado OnCellClick");
    }

    [Fact(DisplayName = "SPEC-0024:E2E-01 — Home.razor.css existe e isola estilos")]
    [Trait("Category", "SPEC-0024:E2E-01")]
    public void HomeCss_ShouldExistAndContainScopedStyles()
    {
        var cssPath = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.css");
        Assert.True(File.Exists(cssPath), "Home.razor.css deve existir");

        var cssContent = File.ReadAllText(cssPath);
        Assert.Contains(".game-container", cssContent);
        Assert.Contains(".board", cssContent);
    }
}
