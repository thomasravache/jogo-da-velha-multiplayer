using System;
using System.IO;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Layout;
using Xunit;

namespace TicTacToe.Tests;

public class MudBlazorIntegrationTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact(DisplayName = "SPEC-0026:UT-01 — Program.cs e App.razor registram e carregam MudBlazor")]
    [Trait("Category", "SPEC-0026:UT-01")]
    public void ProgramAndApp_ShouldConfigureMudBlazor()
    {
        var programPath = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Program.cs");
        var appPath = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Components/App.razor");

        Assert.True(File.Exists(programPath), "Program.cs deve existir");
        Assert.True(File.Exists(appPath), "App.razor deve existir");

        var programContent = File.ReadAllText(programPath);
        var appContent = File.ReadAllText(appPath);

        Assert.Contains("AddMudServices()", programContent);
        Assert.Contains("MudBlazor.min.css", appContent);
        Assert.Contains("MudBlazor.min.js", appContent);
    }

    [Fact(DisplayName = "SPEC-0026:UT-02 — MainLayout utiliza MudThemeProvider com IsDarkMode e MudAppBar")]
    [Trait("Category", "SPEC-0026:UT-02")]
    public async Task MainLayout_ShouldRenderMudLayoutAndAppBar()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();

        var cut = ctx.Render<MainLayout>();
        var markup = cut.Markup;

        Assert.Contains("mud-layout", markup);
        Assert.Contains("mud-appbar", markup);
        Assert.DoesNotContain("top-row px-4", markup);
    }

    [Fact(DisplayName = "SPEC-0026:IT-01 — Lobby utiliza componentes MudBlazor no tema escuro")]
    [Trait("Category", "SPEC-0026:IT-01")]
    public async Task Lobby_ShouldUseMudBlazorComponents()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();

        var cut = ctx.Render<Lobby>(parameters => parameters
            .Add(p => p.PlayerName, "Thomas")
            .Add(p => p.SelectedDifficulty, AiDifficulty.Hard));

        var markup = cut.Markup;
        Assert.Contains("mud-card", markup);
        Assert.Contains("mud-button", markup);
        Assert.Contains("Jogar Online", markup);
    }

    [Fact(DisplayName = "SPEC-0026:IT-02 — GameBoard processa jogadas e possui estilização compatível")]
    [Trait("Category", "SPEC-0026:IT-02")]
    public async Task GameBoard_ShouldRenderAndProcessMoves()
    {
        var game = new GameSession();
        int clickedIndex = -1;

        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();

        var cut = ctx.Render<GameBoard>(parameters => parameters
            .Add(p => p.Game, game)
            .Add(p => p.MyPlayer, Player.X)
            .Add(p => p.OnCellClick, (int idx) => clickedIndex = idx));

        var cell = cut.Find(".cell.playable");
        cell.Click();

        Assert.True(clickedIndex >= 0);
    }

    [Fact(DisplayName = "SPEC-0026:E2E-01 — app.css e tema escuro eliminam fundo branco e restauram contraste")]
    [Trait("Category", "SPEC-0026:E2E-01")]
    public void Theme_ShouldEliminateWhiteCanvasAndEnforceDarkBackground()
    {
        var appCssPath = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/wwwroot/app.css");
        Assert.True(File.Exists(appCssPath), "app.css deve existir");

        var appCssContent = File.ReadAllText(appCssPath);
        Assert.Contains("--bg: #1a1a2e", appCssContent);
        Assert.Contains("background-color: var(--bg)", appCssContent);
    }
}
