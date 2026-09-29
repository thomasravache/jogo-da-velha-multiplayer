using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0041: abandonar partida e pedir revanche — componentes e Home com dois jogadores

public class LeaveAndRematchUiTests
{
    private static IElement? Btn(IRenderedComponent<RematchBar> cut, string text) =>
        cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains(text, StringComparison.Ordinal));

    private static IRenderedComponent<RematchBar> Bar(BunitContext ctx, Action<ComponentParameterCollectionBuilder<RematchBar>> configure) =>
        ctx.Render<RematchBar>(p =>
        {
            p.Add(r => r.Finished, true).Add(r => r.Consent, true);
            configure(p);
        });

    [Fact(DisplayName = "SPEC-0041:UT-07 — RematchBar sem consentimento mantém o botão imediato e sem partida encerrada some")]
    [Trait("Category", "SPEC-0041:UT-07")]
    public void RematchBar_WithoutConsent_KeepsImmediateButton()
    {
        var clicks = 0;
        using var ctx = new BunitContext();
        var cut = ctx.Render<RematchBar>(p => p.Add(r => r.Finished, true).Add(r => r.OnRematch, EventCallback.Factory.Create(this, () => clicks++)));
        cut.Find("button").Click();
        Assert.Equal(1, clicks);
        Assert.Empty(ctx.Render<RematchBar>(p => p.Add(r => r.Finished, false).Add(r => r.Consent, true)).FindAll("button"));
    }

    [Fact(DisplayName = "SPEC-0041:UT-07b — RematchBar com consentimento: pedir, aguardar, aceitar/recusar, recusada, expirada e oponente ausente")]
    [Trait("Category", "SPEC-0041:UT-07")]
    public void RematchBar_WithConsent_ShouldCoverAllStates()
    {
        using var ctx = new BunitContext();
        int requests = 0, accepts = 0, declines = 0;
        void Wire(ComponentParameterCollectionBuilder<RematchBar> p) => p
            .Add(r => r.OnRequest, EventCallback.Factory.Create(this, () => requests++))
            .Add(r => r.OnAccept, EventCallback.Factory.Create(this, () => accepts++))
            .Add(r => r.OnDecline, EventCallback.Factory.Create(this, () => declines++));

        var none = Bar(ctx, Wire);
        Btn(none, "Pedir revanche")!.Click();
        Assert.Equal(1, requests);

        var waiting = Bar(ctx, p => p.Add(r => r.State, RematchState.Requested).Add(r => r.RequestedByMe, true));
        Assert.Contains("Aguardando resposta…", waiting.Markup);
        Assert.Null(Btn(waiting, "Pedir revanche"));

        var incoming = Bar(ctx, p => { Wire(p); p.Add(r => r.State, RematchState.Requested).Add(r => r.RequesterName, "Ana"); });
        Assert.Contains("Ana pediu revanche", incoming.Markup);
        Btn(incoming, "Aceitar")!.Click();
        Btn(incoming, "Recusar")!.Click();
        Assert.Equal((1, 1), (accepts, declines));

        var declined = Bar(ctx, p => p.Add(r => r.State, RematchState.Declined));
        Assert.Contains("Oponente recusou a revanche", declined.Markup);
        Assert.NotNull(Btn(declined, "Pedir revanche"));

        var expired = Bar(ctx, p => p.Add(r => r.State, RematchState.Expired));
        Assert.Contains("expirou", expired.Markup);
        Assert.NotNull(Btn(expired, "Pedir revanche"));

        var left = Bar(ctx, p => p.Add(r => r.OpponentLeft, true));
        Assert.Contains("Oponente saiu da partida", left.Markup);
        Assert.Null(Btn(left, "Pedir revanche"));
        Assert.Equal("polite", left.Find("[aria-live]").GetAttribute("aria-live"));
    }

    [Fact(DisplayName = "SPEC-0041:UT-07c — Oponente ausente esconde 'Próxima rodada' e avisa, mesmo sem consentimento")]
    [Trait("Category", "SPEC-0041:UT-07")]
    public void RematchBar_OpponentLeft_HidesNextRoundEvenWithoutConsent()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<RematchBar>(p => p
            .Add(r => r.Finished, true).Add(r => r.Kind, RematchBar.RematchKind.NextRound).Add(r => r.OpponentLeft, true));

        Assert.Contains("Oponente saiu da partida", cut.Markup);
        Assert.Empty(cut.FindAll("button"));
        Assert.DoesNotContain(ctx.Render<RematchBar>(p => p.Add(r => r.Finished, false)).FindAll("[aria-live]"), e => e.TextContent.Length > 0);
    }

    [Fact(DisplayName = "SPEC-0041:UT-08 — ArenaActions: confirmação em duas etapas, cancelar e Esc")]
    [Trait("Category", "SPEC-0041:UT-08")]
    public void ArenaActions_ShouldConfirmInTwoSteps()
    {
        var leaves = 0;
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<ArenaActions>(p => p.Add(a => a.InProgress, true).Add(a => a.OnLeave, EventCallback.Factory.Create(this, () => leaves++)));

        Assert.Single(cut.FindAll("button"));
        cut.Find("button").Click();
        Assert.Equal(0, leaves);
        Assert.Contains("Confirmar abandono?", cut.Markup);
        Assert.NotNull(cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancelar")));

        cut.FindAll("button").First(b => b.TextContent.Contains("Cancelar")).Click();
        Assert.DoesNotContain("Confirmar abandono?", cut.Markup);
        Assert.Equal(0, leaves);

        cut.Find("button").Click();
        cut.Find("[data-confirm-leave]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.DoesNotContain("Confirmar abandono?", cut.Markup);

        cut.Find("button").Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Confirmar").Click();
        Assert.Equal(1, leaves);

        Assert.Empty(ctx.Render<ArenaActions>(p => p.Add(a => a.InProgress, false)).FindAll("button"));
    }

    [Fact(DisplayName = "SPEC-0041:UT-09 — Avisos: abandono do oponente, saída e recusa em região aria-live")]
    [Trait("Category", "SPEC-0041:UT-09")]
    public void Notices_ShouldBeAnnouncedInLiveRegions()
    {
        using var ctx = new BunitContext();
        using var g = new GameSession(enableBackgroundTimer: false) { Mode = GameMode.Online };
        g.SetPlayerName(Player.X, "Ana");
        g.SetPlayerName(Player.O, "Bia");
        g.MakeMove(0, Player.X);
        g.Leave(Player.O);

        var winner = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, Player.X));
        Assert.Contains("Oponente abandonou. Vitória por W.O.", winner.Find("p[aria-live='polite']").TextContent);
        var loser = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, Player.O));
        Assert.Contains("Você abandonou a partida", loser.Find("p[aria-live='polite']").TextContent);
    }

    [Fact(DisplayName = "SPEC-0041:UT-10 — Voltar ao lobby aparece ao fim da partida e dispara o callback")]
    [Trait("Category", "SPEC-0041:UT-10")]
    public void BackToLobby_ShouldFireCallback()
    {
        var back = 0;
        using var ctx = new BunitContext();
        var cut = Bar(ctx, p => p.Add(r => r.OnBackToLobby, EventCallback.Factory.Create(this, () => back++)));

        Btn(cut, "Voltar ao lobby")!.Click();
        Assert.Equal(1, back);
    }

    // ---------- Home com dois jogadores na mesma partida ----------

    private sealed class Table(BunitContext ctx, DbContextOptions<GameplayDbContext> options, ShellState shell, ConcurrentDictionary<Guid, GameSession> games)
    {
        public BunitContext Ctx { get; } = ctx;
        public DbContextOptions<GameplayDbContext> Options { get; } = options;
        public ShellState Shell { get; } = shell;
        public ConcurrentDictionary<Guid, GameSession> Games { get; } = games;
    }

    private static Table NewTable()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var options = new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var shell = new ShellState();
        var games = new ConcurrentDictionary<Guid, GameSession>();
        ctx.Services.AddSingleton<MatchmakingService>();
        ctx.Services.AddSingleton(games);
        ctx.Services.AddSingleton(shell);
        ctx.Services.AddSingleton<IPlayerStorage>(new InMemoryPlayerStorage());
        ctx.Services.AddScoped<PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return new Table(ctx, options, shell, games);
    }

    private static IRenderedComponent<Home> Join(BunitContext ctx, string name)
    {
        var home = ctx.Render<Home>();
        home.WaitForAssertion(() => home.Find("input#playerName").Input(name));
        home.WaitForAssertion(() => home.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Procurar oponente")).Click());
        return home;
    }

    private static (IRenderedComponent<Home> X, IRenderedComponent<Home> O, GameSession Game) Start(Table t)
    {
        var x = Join(t.Ctx, "Ana");
        var o = Join(t.Ctx, "Bia");
        x.WaitForAssertion(() => Assert.Single(t.Games));
        x.WaitForAssertion(() => Assert.NotEmpty(x.FindAll("[data-cell]")), TimeSpan.FromSeconds(3));
        return (x, o, t.Games.Values.Single());
    }

    private static void Click(IRenderedComponent<Home> home, string text) =>
        home.WaitForAssertion(() => home.FindAll("button").First(b => b.TextContent.Trim().Contains(text, StringComparison.Ordinal)).Click(), TimeSpan.FromSeconds(3));

    [Fact(DisplayName = "SPEC-0041:IT-01 — Abandono: quem sai volta ao lobby, o outro vê a vitória por W.O. e uma única linha é gravada")]
    [Trait("Category", "SPEC-0041:IT-01")]
    public async Task Leave_ShouldForfeitOnce_AndReturnToLobby()
    {
        var t = NewTable();
        await using var _ = t.Ctx;
        var (x, o, game) = Start(t);
        game.MakeMove(0, Player.X);

        Click(x, "Abandonar");
        Click(x, "Confirmar");

        x.WaitForAssertion(() => Assert.Contains("Procurar oponente", x.Markup), TimeSpan.FromSeconds(3));
        Assert.Empty(x.FindAll("[data-cell]")); // saiu da arena (o shell é por circuito e volta ao normal junto)
        o.WaitForAssertion(() => Assert.Contains("Oponente abandonou. Vitória por W.O.", o.Markup), TimeSpan.FromSeconds(3));

        MatchResult[] rows = [];
        for (var i = 0; i < 50 && rows.Length == 0; i++)
        {
            await using var db = new GameplayDbContext(t.Options);
            rows = await db.MatchResults.ToArrayAsync();
            if (rows.Length == 0) await Task.Delay(100);
        }

        var row = Assert.Single(rows);
        Assert.Equal(EndReason.Abandon, row.EndReason);
        Assert.Equal("Bia", row.WinnerName);
    }

    [Fact(DisplayName = "SPEC-0041:IT-02 — Revanche: pedido visível ao outro, aceite reinicia para os dois e recusa avisa quem pediu")]
    [Trait("Category", "SPEC-0041:IT-02")]
    public async Task Rematch_ShouldRequestAcceptAndDecline()
    {
        var t = NewTable();
        await using var _ = t.Ctx;
        var (x, o, game) = Start(t);
        SeriesRulesTests.WinRound(game, Player.X);

        Click(x, "Pedir revanche");
        o.WaitForAssertion(() => Assert.Contains("Ana pediu revanche", o.Markup), TimeSpan.FromSeconds(3));
        x.WaitForAssertion(() => Assert.Contains("Aguardando resposta…", x.Markup), TimeSpan.FromSeconds(3));

        Click(o, "Recusar");
        x.WaitForAssertion(() => Assert.Contains("Oponente recusou a revanche", x.Markup), TimeSpan.FromSeconds(3));

        Click(o, "Pedir revanche");
        x.WaitForAssertion(() => Assert.Contains("Bia pediu revanche", x.Markup), TimeSpan.FromSeconds(3));
        Click(x, "Aceitar");

        Assert.Equal(Player.None, game.Winner);
        x.WaitForAssertion(() => Assert.DoesNotContain("pediu revanche", x.Markup), TimeSpan.FromSeconds(3));
        o.WaitForAssertion(() => Assert.DoesNotContain("Pedir revanche", o.Markup), TimeSpan.FromSeconds(3));
    }

    [Fact(DisplayName = "SPEC-0041:E2E-01 — Jornada: terminar, pedir e aceitar revanche, jogar e abandonar")]
    [Trait("Category", "SPEC-0041:E2E-01")]
    public async Task Journey_ShouldRematchThenLeave()
    {
        var t = NewTable();
        await using var _ = t.Ctx;
        var (x, o, game) = Start(t);
        SeriesRulesTests.WinRound(game, Player.X);
        Click(x, "Pedir revanche");
        Click(o, "Aceitar");
        Assert.Equal(Player.None, game.Winner);

        game.MakeMove(4, Player.X);
        Click(o, "Abandonar");
        Click(o, "Confirmar");

        x.WaitForAssertion(() => Assert.Contains("Oponente abandonou. Vitória por W.O.", x.Markup), TimeSpan.FromSeconds(3));
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(EndReason.Abandon, game.EndReason);
    }
}
