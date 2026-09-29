using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;
using TicTacToe.Web.Services.Presence;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0042: W.O. por desconexão — aviso na arena e dois Home na mesma partida

public class DisconnectUiTests
{
    [Fact(DisplayName = "SPEC-0042:UT-05 — Aviso de oponente desconectado com contagem e vitória por W.O. em região aria-live")]
    [Trait("Category", "SPEC-0042:UT-05")]
    public void Scoreboard_ShouldAnnounceDisconnect()
    {
        var time = new ManualTime();
        using var g = new GameSession(enableBackgroundTimer: false, timeProvider: time) { Mode = GameMode.Online };
        g.SetPlayerName(Player.X, "Ana");
        g.SetPlayerName(Player.O, "Bia");
        using var ctx = new BunitContext();
        IRenderedComponent<Scoreboard> Render(Player me) => ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, me));

        g.MakeMove(0, Player.X);
        Assert.Equal("", Render(Player.X).Find("[data-disconnect-notice]").TextContent.Trim());

        g.SetConnection(Player.O, false);
        time.Advance(TimeSpan.FromSeconds(4));
        var waiting = Render(Player.X).Find("[data-disconnect-notice]");
        Assert.Equal("polite", waiting.GetAttribute("aria-live"));
        Assert.Contains("Oponente desconectado. Aguardando reconexão… 11s", waiting.TextContent);
        Assert.Equal("", Render(Player.O).Find("[data-disconnect-notice]").TextContent.Trim()); // o desconectado não vê o próprio aviso

        time.Advance(TimeSpan.FromSeconds(11));
        g.Tick();
        var ended = Render(Player.X);
        Assert.Contains("Oponente desconectou. Vitória por W.O.", ended.Find("p[aria-live='polite']").TextContent);
        Assert.Equal("", ended.Find("[data-disconnect-notice]").TextContent.Trim());
    }

    private sealed class Table(BunitContext ctx, DbContextOptions<GameplayDbContext> options, ManualTime time, ConcurrentDictionary<Guid, GameSession> games)
    {
        public BunitContext Ctx { get; } = ctx;
        public DbContextOptions<GameplayDbContext> Options { get; } = options;
        public ManualTime Time { get; } = time;
        public ConcurrentDictionary<Guid, GameSession> Games { get; } = games;
    }

    private static Table NewTable()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var options = new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var time = new ManualTime();
        var games = new ConcurrentDictionary<Guid, GameSession>();
        ctx.Services.AddSingleton<MatchmakingService>();
        ctx.Services.AddSingleton(games);
        ctx.Services.AddSingleton<TimeProvider>(time);
        ctx.Services.AddSingleton(new ShellState());
        ctx.Services.AddSingleton<IPlayerStorage>(new InMemoryPlayerStorage());
        ctx.Services.AddScoped<PlayerIdentityService>();
        ctx.Services.AddScoped<MatchPresenceContext>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return new Table(ctx, options, time, games);
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

    [Fact(DisplayName = "SPEC-0042:IT-01 — Queda de um circuito: o outro vê o aviso, depois a vitória por W.O., com uma única linha Disconnect; retorno em 5 s cancela")]
    [Trait("Category", "SPEC-0042:IT-01")]
    public async Task Disconnect_ShouldWarnThenForfeitOnce_AndReturnCancels()
    {
        var t = NewTable();
        await using var _ = t.Ctx;
        var (x, _, game) = Start(t);
        game.MakeMove(0, Player.X);

        game.SetConnection(Player.O, false);
        x.WaitForAssertion(() => Assert.Contains("Oponente desconectado. Aguardando reconexão…", x.Markup), TimeSpan.FromSeconds(3));
        t.Time.Advance(TimeSpan.FromSeconds(5));
        game.SetConnection(Player.O, true);
        x.WaitForAssertion(() => Assert.DoesNotContain("Aguardando reconexão", x.Markup), TimeSpan.FromSeconds(3));
        Assert.Equal(Player.None, game.Winner);

        game.SetConnection(Player.O, false);
        t.Time.Advance(TimeSpan.FromSeconds(15));
        game.Tick();
        x.WaitForAssertion(() => Assert.Contains("Oponente desconectou. Vitória por W.O.", x.Markup), TimeSpan.FromSeconds(3));

        MatchResult[] rows = [];
        for (var i = 0; i < 50 && rows.Length == 0; i++)
        {
            await using var db = new GameplayDbContext(t.Options);
            rows = await db.MatchResults.ToArrayAsync();
            if (rows.Length == 0) await Task.Delay(100);
        }

        var row = Assert.Single(rows);
        Assert.Equal(EndReason.Disconnect, row.EndReason);
        Assert.Equal("Ana", row.WinnerName);
    }

    [Fact(DisplayName = "SPEC-0042:IT-02b — Home associa o jogador à partida humana e desassocia ao ser descartada")]
    [Trait("Category", "SPEC-0042:IT-02")]
    public async Task Home_ShouldAttachAndDetachPresence()
    {
        var t = NewTable();
        await using var _ = t.Ctx;
        var (x, o, game) = Start(t);
        var context = t.Ctx.Services.GetRequiredService<MatchPresenceContext>();
        Assert.Same(game, context.Session); // escopo único no teste: o último Home a entrar anexou

        await t.Ctx.DisposeComponentsAsync();

        Assert.Null(context.Session);
        Assert.NotNull(game.DisconnectSecondsLeft(Player.X) ?? game.DisconnectSecondsLeft(Player.O)); // sair da tela inicia a contagem
    }

    [Fact(DisplayName = "SPEC-0042:E2E-01 — Jornada: queda, contagem visível, W.O. para quem ficou e histórico 'Desconexão do oponente'")]
    [Trait("Category", "SPEC-0042:E2E-01")]
    public async Task Journey_ShouldShowCountdownWalkOverAndHistoryReason()
    {
        var t = NewTable();
        await using var _ = t.Ctx;
        var (x, _, game) = Start(t);
        game.MakeMove(0, Player.X);

        game.SetConnection(Player.O, false);
        t.Time.Advance(TimeSpan.FromSeconds(3));
        game.Tick(); // o tique do relógio da sessão atualiza a contagem na tela
        x.WaitForAssertion(() => Assert.Contains("12s", x.Markup), TimeSpan.FromSeconds(3));

        t.Time.Advance(TimeSpan.FromSeconds(12));
        game.Tick();
        x.WaitForAssertion(() => Assert.Contains("Oponente desconectou. Vitória por W.O.", x.Markup), TimeSpan.FromSeconds(3));

        HistoryPage? page = null;
        for (var i = 0; i < 50 && page?.Items.Count is null or 0; i++)
        {
            await using var db = new GameplayDbContext(t.Options);
            page = await new GameResultService(db, NullLogger<GameResultService>.Instance)
                .GetHistoryAsync(new HistoryQuery(null, HistoryScope.All, HistoryFilter.All, null, HistorySort.Recent, 1));
            if (page.Items.Count == 0) await Task.Delay(100);
        }

        Assert.Equal("Desconexão do oponente", Assert.Single(page!.Items).Reason);
    }
}
