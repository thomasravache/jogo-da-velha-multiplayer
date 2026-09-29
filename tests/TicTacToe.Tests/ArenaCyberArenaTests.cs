using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0031: Arena da partida Cyber Arena

public class ArenaCyberArenaTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string[] WinCells = ["0", "4", "8"];

    private static GameSession NewGame(string x = "Alice", string o = "Bob")
    {
        var game = new GameSession(enableBackgroundTimer: false);
        game.SetPlayerName(Player.X, x);
        game.SetPlayerName(Player.O, o);
        return game;
    }

    private static void Play(GameSession game, params (int Cell, Player Who)[] moves)
    {
        foreach (var (cell, who) in moves) game.MakeMove(cell, who);
    }

    [Fact(DisplayName = "SPEC-0031:UT-01 — Cards dos jogadores com nome, símbolo, placar e tag Você só no local")]
    [Trait("Category", "SPEC-0031:UT-01")]
    public void PlayerCards_ShouldShowNameSymbolScoreAndYouTag()
    {
        var game = NewGame();
        Play(game, (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X));
        game.Restart();

        using var ctx = new BunitContext();
        var cut = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, game).Add(s => s.MyPlayer, Player.X));

        var cards = cut.FindAll("[data-player]");
        Assert.Equal(2, cards.Count);
        var x = cut.Find("[data-player='X']");
        var o = cut.Find("[data-player='O']");
        Assert.Contains("Alice", x.TextContent);
        Assert.Contains("✕", x.TextContent);
        Assert.Contains("1", x.TextContent);
        Assert.Contains("Bob", o.TextContent);
        Assert.Contains("◯", o.TextContent);
        Assert.Contains("Você", x.TextContent);
        Assert.DoesNotContain("Você", o.TextContent);
    }

    [Fact(DisplayName = "SPEC-0031:UT-02 — Card da vez com brilho e o outro com opacidade reduzida")]
    [Trait("Category", "SPEC-0031:UT-02")]
    public void ActiveCard_ShouldGlow_AndOtherShouldFade()
    {
        var game = NewGame();
        using var ctx = new BunitContext();
        var cut = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, game).Add(s => s.MyPlayer, Player.X));

        var x = cut.Find("[data-player='X']");
        var o = cut.Find("[data-player='O']");
        Assert.Equal("true", x.GetAttribute("data-active"));
        Assert.Contains("shadow-[0_0_20px_rgb(255_71_87", x.InnerHtml + x.GetAttribute("class"));
        Assert.Contains("opacity-60", o.GetAttribute("class"));

        game.MakeMove(4, Player.X);
        cut.Render(p => p.Add(s => s.Game, game).Add(s => s.MyPlayer, Player.X));
        Assert.Equal("true", cut.Find("[data-player='O']").GetAttribute("data-active"));
        Assert.Contains("opacity-60", cut.Find("[data-player='X']").GetAttribute("class"));
    }

    [Fact(DisplayName = "SPEC-0031:UT-03 — Faixa de status para cada situação da partida")]
    [Trait("Category", "SPEC-0031:UT-03")]
    public void Status_ShouldDescribeEachSituation()
    {
        using var ctx = new BunitContext();
        string Status(GameSession g, Player me)
        {
            var cut = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, me));
            var region = cut.Find("[aria-live='polite']");
            return region.TextContent;
        }

        var running = NewGame();
        Assert.Contains("Sua vez, Alice!", Status(running, Player.X));
        Assert.Contains("Vez de Alice", Status(running, Player.O));

        var won = NewGame();
        Play(won, (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X));
        Assert.Contains("Você venceu, Alice!", Status(won, Player.X));
        Assert.Contains("Alice venceu!", Status(won, Player.O));

        var draw = NewGame();
        Play(draw, (0, Player.X), (1, Player.O), (2, Player.X), (4, Player.O), (3, Player.X), (5, Player.O), (7, Player.X), (6, Player.O), (8, Player.X));
        Assert.Contains("Deu velha!", Status(draw, Player.X));
    }

    [Fact(DisplayName = "SPEC-0031:UT-04 — Células acessíveis: jogar por clique ou teclado só na vez e em casa vazia")]
    [Trait("Category", "SPEC-0031:UT-04")]
    public void Cells_ShouldBeAccessibleAndPlayableOnlyOnMyTurn()
    {
        var game = NewGame();
        var clicked = -1;
        using var ctx = new BunitContext();
        var cut = ctx.Render<GameBoard>(p => p
            .Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X)
            .Add(b => b.OnCellClick, EventCallback.Factory.Create<int>(this, i => clicked = i)));

        var cell0 = cut.Find("[data-cell='0']");
        Assert.Equal("button", cell0.GetAttribute("role"));
        Assert.Equal("0", cell0.GetAttribute("tabindex"));
        Assert.Equal("Casa 1, vazia", cell0.GetAttribute("aria-label"));

        cell0.Click();
        Assert.Equal(0, clicked);

        clicked = -1;
        cut.Find("[data-cell='4']").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(4, clicked);
        clicked = -1;
        cut.Find("[data-cell='5']").KeyDown(new KeyboardEventArgs { Key = " " });
        Assert.Equal(5, clicked);

        // Fora da vez: nada acontece e a célula não é focável.
        var rival = ctx.Render<GameBoard>(p => p
            .Add(b => b.Game, game).Add(b => b.MyPlayer, Player.O)
            .Add(b => b.OnCellClick, EventCallback.Factory.Create<int>(this, i => clicked = i)));
        clicked = -1;
        rival.Find("[data-cell='0']").Click();
        Assert.Equal(-1, clicked);
        Assert.Equal("-1", rival.Find("[data-cell='0']").GetAttribute("tabindex"));

        // Célula ocupada.
        game.MakeMove(4, Player.X);
        var after = ctx.Render<GameBoard>(p => p
            .Add(b => b.Game, game).Add(b => b.MyPlayer, Player.O)
            .Add(b => b.OnCellClick, EventCallback.Factory.Create<int>(this, i => clicked = i)));
        after.Find("[data-cell='4']").Click();
        Assert.Equal(-1, clicked);
        Assert.Equal("Casa 5, X", after.Find("[data-cell='4']").GetAttribute("aria-label"));
    }

    [Fact(DisplayName = "SPEC-0031:UT-06 — Só as três células da linha vencedora recebem data-win")]
    [Trait("Category", "SPEC-0031:UT-06")]
    public void WinningCells_ShouldBeHighlighted()
    {
        var game = NewGame();
        Play(game, (0, Player.X), (1, Player.O), (4, Player.X), (2, Player.O), (8, Player.X));
        using var ctx = new BunitContext();
        var cut = ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X));

        var winning = cut.FindAll("[data-win='true']").Select(c => c.GetAttribute("data-cell")).OrderBy(x => x).ToArray();
        Assert.Equal(WinCells, winning);
    }

    [Fact(DisplayName = "SPEC-0031:UT-07 — Timer por faixas de cor, rótulo em segundos e ausência ao fim")]
    [Trait("Category", "SPEC-0031:UT-07")]
    public void Timer_ShouldChangeToneByRemainingTime()
    {
        var game = NewGame();
        using var ctx = new BunitContext();

        var full = ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X));
        Assert.Equal("success", full.Find("[role='progressbar']").GetAttribute("data-tone"));
        Assert.Contains("15s restantes", full.Markup);

        for (var i = 0; i < 8; i++) game.Tick();
        var warn = ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X));
        Assert.Equal("warning", warn.Find("[role='progressbar']").GetAttribute("data-tone"));
        Assert.Contains("07s restantes", warn.Markup);

        for (var i = 0; i < 5; i++) game.Tick();
        var danger = ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X));
        Assert.Equal("danger", danger.Find("[role='progressbar']").GetAttribute("data-tone"));
        Assert.Contains("02s restantes", danger.Markup);

        var won = NewGame();
        Play(won, (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X));
        var ended = ctx.Render<GameBoard>(p => p.Add(b => b.Game, won).Add(b => b.MyPlayer, Player.X));
        Assert.Empty(ended.FindAll("[role='progressbar']"));
    }

    [Fact(DisplayName = "SPEC-0031:UT-08 — Banner de W.O. cita o vencedor")]
    [Trait("Category", "SPEC-0031:UT-08")]
    public void TimeoutBanner_ShouldNameWinner()
    {
        var game = NewGame("Thomas", "Ana");
        for (var i = 0; i < GameSession.DefaultTurnTimeSeconds; i++) game.Tick();
        using var ctx = new BunitContext();
        var cut = ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X));

        Assert.Contains("Tempo esgotado! Vitória por W.O. para Ana", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0031:UT-09 — RematchBar só aparece ao fim e dispara o reinício")]
    [Trait("Category", "SPEC-0031:UT-09")]
    public void RematchBar_ShouldRenderOnlyWhenFinished()
    {
        var clicks = 0;
        using var ctx = new BunitContext();
        var hidden = ctx.Render<RematchBar>(p => p.Add(r => r.Finished, false));
        Assert.Empty(hidden.FindAll("button"));

        var shown = ctx.Render<RematchBar>(p => p
            .Add(r => r.Finished, true)
            .Add(r => r.OnRematch, EventCallback.Factory.Create(this, () => clicks++)));
        var button = shown.FindAll("button").Single();
        Assert.Contains("Jogar novamente", button.TextContent);
        button.Click();
        Assert.Equal(1, clicks);
    }

    [Fact(DisplayName = "SPEC-0031:UT-10 — Confete usa a paleta dos tokens")]
    [Trait("Category", "SPEC-0031:UT-10")]
    public void Confetti_ShouldUseTokenPalette()
    {
        var js = File.ReadAllText(Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/wwwroot/app.js"));
        foreach (var hex in new[] { "#ff4757", "#00d2d3", "#f39c12", "#10b981" })
            Assert.Contains(hex, js);
        Assert.DoesNotContain("#e74c3c", js);
        Assert.DoesNotContain("#3498db", js);
    }

    private static BunitContext NewHomeContext(out ShellState shell, out ConcurrentDictionary<Guid, GameSession> games)
    {
        var options = new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<MatchmakingService>();
        games = new ConcurrentDictionary<Guid, GameSession>();
        ctx.Services.AddSingleton(games);
        shell = new ShellState();
        ctx.Services.AddSingleton(shell);
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return ctx;
    }

    private static void StartSolo(IRenderedComponent<Home> home, string name = "Thomas")
    {
        home.Find("#playerName").Input(name);
        home.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Iniciar partida solo")).Click();
    }

    [Fact(DisplayName = "SPEC-0031:UT-11 — Layout da arena: três colunas a partir de lg e empilhado abaixo")]
    [Trait("Category", "SPEC-0031:UT-11")]
    public async Task ArenaLayout_ShouldBeThreeColumnsFromLg()
    {
        await using var ctx = NewHomeContext(out _, out _);
        var home = ctx.Render<Home>();
        StartSolo(home);

        var layout = home.Find(".game-container").GetAttribute("class") ?? "";
        Assert.Contains("grid", layout);
        Assert.Contains("lg:grid-cols-", layout);
        Assert.Contains("grid-cols-", layout);

        // Posição dos filhos no desktop: cards nas laterais, tabuleiro ao centro.
        Assert.Contains("lg:col-start-1", home.Find("[data-player='X']").GetAttribute("class"));
        Assert.Contains("lg:col-start-3", home.Find("[data-player='O']").GetAttribute("class"));
        Assert.Contains("lg:col-start-2", home.Find(".board").ParentElement!.GetAttribute("class"));
    }

    [Fact(DisplayName = "SPEC-0031:UT-12 — Componentes da arena sem MudBlazor e Home com título XO Arena")]
    [Trait("Category", "SPEC-0031:UT-12")]
    public async Task ArenaComponents_ShouldNotUseMudBlazor()
    {
        var game = NewGame();
        await using var ctx = new BunitContext();
        var markups = new[]
        {
            ctx.Render<Scoreboard>(p => p.Add(s => s.Game, game).Add(s => s.MyPlayer, Player.X)).Markup,
            ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X)).Markup,
            ctx.Render<PlayerCard>(p => p.Add(c => c.Player, Player.X).Add(c => c.Name, "A")).Markup,
            ctx.Render<RematchBar>(p => p.Add(r => r.Finished, true)).Markup,
        };
        Assert.All(markups, m => Assert.DoesNotContain("mud-", m));

        var home = File.ReadAllText(Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor"));
        Assert.Contains("<PageTitle>XO Arena</PageTitle>", home);
    }

    [Fact(DisplayName = "SPEC-0031:IT-01 — Home ativa o modo imersivo na partida e restaura ao sair")]
    [Trait("Category", "SPEC-0031:IT-01")]
    public async Task Home_ShouldToggleImmersiveShell()
    {
        await using var ctx = NewHomeContext(out var shell, out _);
        var home = ctx.Render<Home>();
        Assert.False(shell.Immersive);

        StartSolo(home);
        Assert.True(shell.Immersive);
        Assert.Equal("Partida ativa", shell.Title);

        home.Instance.Dispose();
        Assert.False(shell.Immersive);
    }

    [Fact(DisplayName = "SPEC-0031:E2E-01 — Jornada da partida solo: cards, tabuleiro, timer, status e primeira jogada")]
    [Trait("Category", "SPEC-0031:E2E-01")]
    public async Task SoloJourney_ShouldShowArenaAndAcceptFirstMove()
    {
        await using var ctx = NewHomeContext(out _, out _);
        var home = ctx.Render<Home>();
        StartSolo(home);

        Assert.Equal(2, home.FindAll("[data-player]").Count);
        Assert.Equal(9, home.FindAll("[data-cell]").Count);
        Assert.Single(home.FindAll("[role='progressbar']"));
        Assert.Contains("Sua vez, Thomas!", home.Find("[aria-live='polite']").TextContent);

        home.Find("[data-cell='4']").Click();
        home.WaitForAssertion(() => Assert.Equal("X", home.Find("[data-cell='4']").GetAttribute("data-mark")));
    }

    [Fact(DisplayName = "SPEC-0031:UT-04b — Prévia da marca no hover e aria-disabled nas células não jogáveis")]
    [Trait("Category", "SPEC-0031:UT-04")]
    public void Cells_ShouldShowGhostMark_AndExposeDisabledState()
    {
        var game = NewGame();
        game.MakeMove(4, Player.X);
        using var ctx = new BunitContext();

        var mine = ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.O));
        Assert.Equal("O", mine.Find("[data-cell='0'] [data-ghost]").GetAttribute("data-ghost"));
        Assert.Empty(mine.FindAll("[data-cell='4'] [data-ghost]"));
        Assert.Null(mine.Find("[data-cell='0']").GetAttribute("aria-disabled"));
        Assert.Equal("true", mine.Find("[data-cell='4']").GetAttribute("aria-disabled"));

        var rival = ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X));
        Assert.Empty(rival.FindAll("[data-ghost]"));
        Assert.Equal("true", rival.Find("[data-cell='0']").GetAttribute("aria-disabled"));
    }
}
