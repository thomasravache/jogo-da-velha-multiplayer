using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Chess;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.Chess;
using TicTacToe.Web.Services.PlayerIdentity;
using TicTacToe.Web.Services.Presence;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0060: abandono, revanche e desconexão no xadrez (arena e página com dois jogadores)

/// <summary>Dois (ou mais) circuitos sobre os mesmos serviços singleton, com presença por circuito.</summary>
internal sealed class ChessDuoHarness : IDisposable
{
    public MatchmakingService Matchmaking { get; } = new();

    public ChessMatchRegistry Registry { get; } = new();

    public ManualTime Time { get; } = new();

    public DbContextOptions<GameplayDbContext> Options { get; } =
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    public List<BunitContext> Contexts { get; } = [];

    public void Dispose()
    {
        foreach (var ctx in Contexts)
        {
            ctx.Dispose();
        }
    }

    public BunitContext NewCircuit(string nick)
    {
        var storage = new InMemoryPlayerStorage();
        storage.Data["xo.player"] = JsonSerializer.Serialize(new { id = Guid.NewGuid(), nick });
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(Matchmaking);
        ctx.Services.AddSingleton(Registry);
        ctx.Services.AddSingleton<TimeProvider>(Time);
        var flips = 0;
        ctx.Services.AddSingleton<Func<bool>>(() => Interlocked.Increment(ref flips) % 2 == 1);
        ctx.Services.AddSingleton(new ShellState());
        ctx.Services.AddSingleton<IPlayerStorage>(storage);
        ctx.Services.AddScoped<PlayerIdentityService>();
        ctx.Services.AddScoped<MatchPresenceContext>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(Options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        ctx.Services.AddScoped(sp => new ChessResultRecorder(sp.GetRequiredService<GameResultService>(), NullLogger<ChessResultRecorder>.Instance));
        Contexts.Add(ctx);
        return ctx;
    }

    public IRenderedComponent<ChessHome> Open(string nick)
    {
        var ctx = NewCircuit(nick);
        var cut = ctx.Render<ChessHome>();
        cut.WaitForAssertion(() => Assert.Equal(nick, cut.Find("input#playerName").GetAttribute("value")));
        return cut;
    }

    /// <summary>Ana (brancas) e Bia (pretas) pareadas em Blitz.</summary>
    public (IRenderedComponent<ChessHome> A, IRenderedComponent<ChessHome> B) Pair()
    {
        var a = Open("Ana");
        var b = Open("Bia");
        Search(a, "Brancas");
        Search(b, "Pretas");
        a.WaitForAssertion(() => Assert.NotEmpty(a.FindComponents<ChessArena>()));
        b.WaitForAssertion(() => Assert.NotEmpty(b.FindComponents<ChessArena>()));
        return (a, b);
    }

    private static void Search(IRenderedComponent<ChessHome> cut, string color)
    {
        cut.FindAll("[role='radio']").First(r => r.TextContent.Contains("Blitz", StringComparison.Ordinal)).Click();
        cut.FindAll("[role='radio']").First(r => r.TextContent.Contains(color, StringComparison.Ordinal)).Click();
        Button(cut, "Procurar oponente").Click();
    }

    public static IElement Button(IRenderedComponent<ChessHome> cut, string text) =>
        cut.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains(text, StringComparison.Ordinal));

    public static bool HasButton(IRenderedComponent<ChessHome> cut, string text) =>
        cut.FindAll("button").Any(b => !b.HasAttribute("role") && b.TextContent.Contains(text, StringComparison.Ordinal));

    public static ChessSession SessionOf(IRenderedComponent<ChessHome> cut) => cut.FindComponent<ChessArena>().Instance.Session;

    public static int SeatOf(IRenderedComponent<ChessHome> cut) => cut.FindComponent<ChessArena>().Instance.MySeat;

    public static PieceColor MyColor(IRenderedComponent<ChessHome> cut) => SessionOf(cut).ColorOf(SeatOf(cut));

    public static string Text(IRenderedComponent<ChessHome> cut) => cut.Markup;

    public static string FirstSquare(IRenderedComponent<ChessHome> cut) =>
        cut.FindAll("button[data-square]")[0].GetAttribute("data-square")!;

    /// <summary>Mate do pastor às avessas (mate do louco): as pretas vencem em quatro lances.</summary>
    public static void FoolsMate(ChessSession session) => ChessSessionTests.Line(session, "f2f3", "e7e5", "g2g4", "d8h4");

    /// <summary>Simula o circuito do jogador caindo (false) ou voltando (true).</summary>
    public static async Task CircuitAsync(BunitContext ctx, bool up)
    {
        var handler = new MatchPresenceCircuitHandler(ctx.Services.GetRequiredService<MatchPresenceContext>());
        Circuit circuit = null!;
        if (up)
        {
            await handler.OnConnectionUpAsync(circuit, CancellationToken.None);
        }
        else
        {
            await handler.OnConnectionDownAsync(circuit, CancellationToken.None);
        }
    }

    public async Task<List<MatchResult>> RowsAsync()
    {
        await using var db = new GameplayDbContext(Options);
        return [.. await db.MatchResults.OrderBy(r => r.PlayedAt).ToListAsync()];
    }

    public async Task WaitForRowsAsync(int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && (await RowsAsync()).Count < expected)
        {
            await Task.Delay(25);
        }
    }
}

public sealed class ChessLeaveAndRematchTests : IDisposable
{
    private readonly ChessDuoHarness _h = new();

    public void Dispose() => _h.Dispose();

    private static string Normalize(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

    private static IRenderedComponent<ChessArena> Render(BunitContext ctx, ChessSession session, int seat = 0, Action<ComponentParameterCollectionBuilder<ChessArena>>? more = null) =>
        ctx.Render<ChessArena>(p =>
        {
            p.Add(c => c.Session, session).Add(c => c.MySeat, seat);
            more?.Invoke(p);
        });

    private static IElement? Btn(IRenderedComponent<ChessArena> cut, string text) =>
        cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains(text, StringComparison.Ordinal));

    private static EventCallback Count(object receiver, Action action) => EventCallback.Factory.Create(receiver, action);

    private static BunitContext NewArenaContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    // ---------- UT-01 ----------

    [Fact(DisplayName = "SPEC-0060:UT-01 — Abandonar abre a confirmação; Cancelar e Esc fecham e devolvem o foco; Confirmar chama o abandono")]
    [Trait("Category", "SPEC-0060:UT-01")]
    public void Arena_ShouldConfirmLeaveInTwoSteps()
    {
        using var ctx = NewArenaContext();
        using var session = ChessSessionTests.New();
        var leaves = 0;
        var cut = Render(ctx, session, more: p => p.Add(c => c.OnLeave, Count(this, () => leaves++)));

        Assert.Empty(cut.FindAll("[data-confirm-leave]"));
        Btn(cut, "Abandonar")!.Click();
        Assert.Single(cut.FindAll("[data-confirm-leave]"));
        Assert.Equal(0, leaves);

        Btn(cut, "Cancelar")!.Click();
        Assert.Empty(cut.FindAll("[data-confirm-leave]"));
        Assert.NotNull(Btn(cut, "Abandonar"));
        Assert.Contains(ctx.JSInterop.Invocations, i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        Btn(cut, "Abandonar")!.Click();
        cut.Find("[data-confirm-leave]").KeyDown("Escape");
        Assert.Empty(cut.FindAll("[data-confirm-leave]"));
        Assert.Equal(0, leaves);

        Btn(cut, "Abandonar")!.Click();
        Btn(cut, "Confirmar")!.Click();
        Assert.Equal(1, leaves);
    }

    [Fact(DisplayName = "SPEC-0060:UT-01 — Abandonar só aparece durante a partida humana (não no fim nem em solo)")]
    [Trait("Category", "SPEC-0060:UT-01")]
    public void Arena_ShouldHideLeaveWhenOverOrSolo()
    {
        using var ctx = NewArenaContext();
        using var session = ChessSessionTests.New();
        using var solo = ChessSessionTests.New();
        solo.Mode = ChessMode.Solo;

        var soloCut = Render(ctx, solo);
        Assert.Null(Btn(soloCut, "Abandonar"));

        var cut = Render(ctx, session);
        Assert.NotNull(Btn(cut, "Abandonar"));
        Assert.True(session.Forfeit(PieceColor.Black, ChessEndReason.Resignation));
        cut.WaitForAssertion(() => Assert.Null(Btn(cut, "Abandonar")));
    }

    // ---------- UT-02 ----------

    [Fact(DisplayName = "SPEC-0060:UT-02 — A barra de revanche cobre pedido, recebido, recusada, expirada e oponente ausente")]
    [Trait("Category", "SPEC-0060:UT-02")]
    public void Arena_ShouldMapRematchStatesToBar()
    {
        var time = new ManualTime();
        using var ctx = NewArenaContext();
        using var session = ChessSessionTests.New(time);
        int requests = 0, accepts = 0, declines = 0;
        void Wire(ComponentParameterCollectionBuilder<ChessArena> p) => p
            .Add(c => c.OnRequestRematch, Count(this, () => requests++))
            .Add(c => c.OnAcceptRematch, Count(this, () => accepts++))
            .Add(c => c.OnDeclineRematch, Count(this, () => declines++));
        var white = Render(ctx, session, 0, Wire);
        var black = Render(ctx, session, 1, Wire);

        Assert.Null(Btn(white, "Pedir revanche")); // partida em andamento: sem barra
        Assert.True(session.Forfeit(PieceColor.Black, ChessEndReason.Resignation));
        white.WaitForAssertion(() => Assert.NotNull(Btn(white, "Pedir revanche")));
        Btn(white, "Pedir revanche")!.Click();
        Assert.Equal(1, requests);

        Assert.True(session.RequestRematch(PieceColor.White));
        white.WaitForAssertion(() => Assert.Contains("Aguardando resposta…", white.Markup, StringComparison.Ordinal));
        Assert.Null(Btn(white, "Pedir revanche"));
        black.WaitForAssertion(() => Assert.Contains("Ana pediu revanche", black.Markup, StringComparison.Ordinal));
        Btn(black, "Aceitar")!.Click();
        Btn(black, "Recusar")!.Click();
        Assert.Equal((1, 1), (accepts, declines));

        Assert.True(session.DeclineRematch(PieceColor.Black));
        white.WaitForAssertion(() => Assert.Contains("Oponente recusou a revanche", white.Markup, StringComparison.Ordinal));
        Assert.NotNull(Btn(white, "Pedir revanche"));

        Assert.True(session.RequestRematch(PieceColor.White));
        time.Advance(TimeSpan.FromSeconds(31));
        session.Tick();
        white.WaitForAssertion(() => Assert.Contains("O pedido de revanche expirou", white.Markup, StringComparison.Ordinal));

        session.Leave(PieceColor.Black);
        white.WaitForAssertion(() => Assert.Contains("Oponente saiu da partida", white.Markup, StringComparison.Ordinal));
        Assert.Null(Btn(white, "Pedir revanche"));
    }

    [Fact(DisplayName = "SPEC-0060:UT-02 — Fim da partida oferece Voltar ao lobby; em solo o botão reinicia na hora sem consentimento")]
    [Trait("Category", "SPEC-0060:UT-02")]
    public void Arena_ShouldOfferBackToLobbyAndImmediateSoloRematch()
    {
        using var ctx = NewArenaContext();
        using var solo = ChessSessionTests.New();
        solo.Mode = ChessMode.Solo;
        int rematches = 0, lobbies = 0;
        var cut = Render(ctx, solo, more: p => p
            .Add(c => c.OnRematch, Count(this, () => rematches++))
            .Add(c => c.OnBackToLobby, Count(this, () => lobbies++)));
        Assert.True(solo.Forfeit(PieceColor.Black, ChessEndReason.Resignation));

        cut.WaitForAssertion(() => Assert.NotNull(Btn(cut, "Jogar novamente")));
        Assert.Null(Btn(cut, "Pedir revanche"));
        Btn(cut, "Jogar novamente")!.Click();
        Btn(cut, "Voltar ao lobby")!.Click();

        Assert.Equal((1, 1), (rematches, lobbies));
    }

    // ---------- UT-03 ----------

    [Fact(DisplayName = "SPEC-0060:UT-03 — Abandono: textos do W.O. na região viva para quem ficou e para quem saiu")]
    [Trait("Category", "SPEC-0060:UT-03")]
    public void Arena_ShouldAnnounceAbandon()
    {
        using var ctx = NewArenaContext();
        using var session = ChessSessionTests.New();
        var white = Render(ctx, session, 0);
        var black = Render(ctx, session, 1);

        Assert.Equal(ChessLeaveResult.Forfeited, session.Leave(PieceColor.Black));

        white.WaitForAssertion(() => Assert.Contains("Oponente abandonou. Vitória por W.O.", Normalize(white.Find("[data-arena-notices]").TextContent), StringComparison.Ordinal));
        black.WaitForAssertion(() => Assert.Contains("Você abandonou a partida", Normalize(black.Find("[data-arena-notices]").TextContent), StringComparison.Ordinal));
        Assert.Equal("polite", white.Find("[data-arena-notices]").GetAttribute("aria-live"));
    }

    [Fact(DisplayName = "SPEC-0060:UT-03 — Desconexão: aviso estático na região, contagem à parte em aria-hidden e W.O. ao fim")]
    [Trait("Category", "SPEC-0060:UT-03")]
    public void Arena_ShouldAnnounceDisconnectAndWalkOver()
    {
        var time = new ManualTime();
        using var ctx = NewArenaContext();
        using var session = ChessSessionTests.New(time);
        var white = Render(ctx, session, 0);
        var black = Render(ctx, session, 1);
        Assert.Empty(white.FindAll("[data-disconnect-notice]"));

        session.SetConnection(PieceColor.Black, false);

        white.WaitForAssertion(() => Assert.Contains("Oponente desconectado. Aguardando reconexão…", Normalize(white.Find("[data-disconnect-notice]").TextContent), StringComparison.Ordinal));
        var region = white.Find("[data-disconnect-notice]");
        Assert.Equal("polite", region.GetAttribute("aria-live"));
        var count = white.Find("[data-disconnect-count]");
        Assert.Equal("true", count.GetAttribute("aria-hidden"));
        Assert.Equal("15s", count.TextContent.Trim());
        Assert.False(region.Contains(count));
        Assert.False(white.Find("[data-arena-notices]").Contains(count));
        Assert.DoesNotContain("15s", region.TextContent, StringComparison.Ordinal);
        Assert.Empty(black.FindAll("[data-disconnect-notice]")); // quem caiu não vê o aviso do oponente

        time.Advance(TimeSpan.FromSeconds(4)); // dispara o pulso de 1 s da arena
        white.WaitForAssertion(() => Assert.Equal("11s", white.Find("[data-disconnect-count]").TextContent.Trim()));

        time.Advance(TimeSpan.FromSeconds(11));
        session.Tick();
        white.WaitForAssertion(() => Assert.Contains("Oponente desconectou. Vitória por W.O.", Normalize(white.Find("[data-arena-notices]").TextContent), StringComparison.Ordinal));
        black.WaitForAssertion(() => Assert.Contains("Você foi desconectado. Derrota por W.O.", Normalize(black.Find("[data-arena-notices]").TextContent), StringComparison.Ordinal));
        Assert.Empty(white.FindAll("[data-disconnect-notice]"));
        Assert.Empty(white.FindAll("[data-disconnect-count]"));
    }

    // ---------- UT-05 ----------

    [Fact(DisplayName = "SPEC-0060:UT-05 — Abandonar e Voltar ao lobby retornam ao lobby, restauram o shell e removem a sessão só quando os dois saíram")]
    [Trait("Category", "SPEC-0060:UT-05")]
    public void Home_ShouldReturnToLobbyAndRemoveSessionWhenBothLeft()
    {
        var (a, b) = _h.Pair();
        var session = ChessDuoHarness.SessionOf(a);
        var shellA = _h.Contexts[0].Services.GetRequiredService<ShellState>();
        a.WaitForAssertion(() => Assert.True(shellA.Immersive));

        ChessDuoHarness.Button(a, "Abandonar").Click();
        ChessDuoHarness.Button(a, "Confirmar").Click();

        a.WaitForAssertion(() => Assert.NotNull(a.Find("input#playerName")));
        Assert.Empty(a.FindComponents<ChessArena>());
        Assert.False(shellA.Immersive);
        Assert.True(_h.Registry.TryGet(FindMatchId(_h, session), out _)); // o outro ainda usa a sessão
        Assert.True(session.IsOver);

        b.WaitForAssertion(() => Assert.True(ChessDuoHarness.HasButton(b, "Voltar ao lobby")));
        ChessDuoHarness.Button(b, "Voltar ao lobby").Click();
        Assert.NotNull(b.Find("input#playerName"));
        Assert.False(_h.Registry.TryGet(FindMatchId(_h, session), out _)); // os dois saíram: removida
    }

    // O registro só expõe TryGet por id do pareamento (não o Id da sessão): procura a entrada da sessão.
    private static Guid FindMatchId(ChessDuoHarness h, ChessSession session)
    {
        var field = typeof(ChessMatchRegistry).GetField("_matches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var map = (System.Collections.IDictionary)field.GetValue(h.Registry)!;
        foreach (System.Collections.DictionaryEntry entry in map)
        {
            var lazy = entry.Value!;
            var match = (ChessMatch)lazy.GetType().GetProperty("Value")!.GetValue(lazy)!;
            if (ReferenceEquals(match.Session, session))
            {
                return (Guid)entry.Key;
            }
        }

        return Guid.NewGuid(); // já removida: um id que não existe
    }

    // ---------- IT-01 ----------

    [Fact(DisplayName = "SPEC-0060:IT-01 — Um abandona: volta ao lobby, o outro vê o W.O. e uma única linha Abandon é gravada")]
    [Trait("Category", "SPEC-0060:IT-01")]
    public async Task Abandon_ShouldReachOpponentAndRecordOnce()
    {
        var (a, b) = _h.Pair();
        ChessSessionTests.Line(ChessDuoHarness.SessionOf(a), "e2e4");

        ChessDuoHarness.Button(a, "Abandonar").Click();
        ChessDuoHarness.Button(a, "Confirmar").Click();

        Assert.NotNull(a.Find("input#playerName"));
        b.WaitForAssertion(() => Assert.Contains("Oponente abandonou. Vitória por W.O.", ChessDuoHarness.Text(b), StringComparison.Ordinal));
        await _h.WaitForRowsAsync(1);
        await Task.Delay(100);
        var row = Assert.Single(await _h.RowsAsync());
        Assert.Equal(EndReason.Abandon, row.EndReason);
        Assert.Equal("O", row.WinnerSide); // Ana (brancas) abandonou: pretas vencem
        Assert.Equal("Bia", row.WinnerName);

        ChessDuoHarness.Button(b, "Voltar ao lobby").Click();
        await Task.Delay(100);
        Assert.Single(await _h.RowsAsync());
    }

    // ---------- IT-02 ----------

    [Fact(DisplayName = "SPEC-0060:IT-02 — Revanche em duas telas: pedir, recusar, pedir de novo e aceitar troca as cores e vira os tabuleiros")]
    [Trait("Category", "SPEC-0060:IT-02")]
    public async Task Rematch_ShouldSyncStatesSwapColorsAndFlipBoards()
    {
        var (a, b) = _h.Pair();
        var session = ChessDuoHarness.SessionOf(a);
        Assert.Equal("a8", ChessDuoHarness.FirstSquare(a));
        Assert.Equal("h1", ChessDuoHarness.FirstSquare(b));
        ChessDuoHarness.FoolsMate(session);
        a.WaitForAssertion(() => Assert.True(ChessDuoHarness.HasButton(a, "Pedir revanche")));
        b.WaitForAssertion(() => Assert.True(ChessDuoHarness.HasButton(b, "Pedir revanche")));

        ChessDuoHarness.Button(a, "Pedir revanche").Click();
        a.WaitForAssertion(() => Assert.Contains("Aguardando resposta…", ChessDuoHarness.Text(a), StringComparison.Ordinal));
        b.WaitForAssertion(() => Assert.Contains("Ana pediu revanche", ChessDuoHarness.Text(b), StringComparison.Ordinal));

        ChessDuoHarness.Button(b, "Recusar").Click();
        a.WaitForAssertion(() => Assert.Contains("Oponente recusou a revanche", ChessDuoHarness.Text(a), StringComparison.Ordinal));

        ChessDuoHarness.Button(b, "Pedir revanche").Click();
        a.WaitForAssertion(() => Assert.Contains("Bia pediu revanche", ChessDuoHarness.Text(a), StringComparison.Ordinal));
        ChessDuoHarness.Button(a, "Aceitar").Click();

        a.WaitForAssertion(() => Assert.False(session.IsOver));
        Assert.Equal(PieceColor.Black, ChessDuoHarness.MyColor(a));
        Assert.Equal(PieceColor.White, ChessDuoHarness.MyColor(b));
        a.WaitForAssertion(() => Assert.Equal("h1", ChessDuoHarness.FirstSquare(a)));
        b.WaitForAssertion(() => Assert.Equal("a8", ChessDuoHarness.FirstSquare(b)));
        await _h.WaitForRowsAsync(1);
        Assert.Single(await _h.RowsAsync()); // a primeira partida foi gravada antes da revanche

        // Um abandono logo depois é atribuído a quem abandonou (Ana, agora de pretas).
        ChessDuoHarness.Button(a, "Abandonar").Click();
        ChessDuoHarness.Button(a, "Confirmar").Click();
        b.WaitForAssertion(() => Assert.Contains("Oponente abandonou. Vitória por W.O.", ChessDuoHarness.Text(b), StringComparison.Ordinal));
        await _h.WaitForRowsAsync(2);
        var rows = await _h.RowsAsync();
        Assert.Equal(2, rows.Count);
        var last = rows[^1];
        Assert.Equal(EndReason.Abandon, last.EndReason);
        Assert.Equal("Bia", last.PlayerXName); // Bia joga de brancas na segunda partida
        Assert.Equal("X", last.WinnerSide);
    }

    // ---------- E2E-01 ----------

    [Fact(DisplayName = "SPEC-0060:E2E-01 — Jornada: revanche, lance e abandono; em outra partida a queda dá W.O. e o histórico mostra 'Desconexão do oponente'")]
    [Trait("Category", "SPEC-0060:E2E-01")]
    public async Task Journey_ShouldRematchAbandonAndDisconnect()
    {
        var (a, b) = _h.Pair();
        var session = ChessDuoHarness.SessionOf(a);
        ChessDuoHarness.FoolsMate(session);
        b.WaitForAssertion(() => Assert.True(ChessDuoHarness.HasButton(b, "Pedir revanche")));
        ChessDuoHarness.Button(b, "Pedir revanche").Click();
        a.WaitForAssertion(() => Assert.True(ChessDuoHarness.HasButton(a, "Aceitar")));
        ChessDuoHarness.Button(a, "Aceitar").Click();
        a.WaitForAssertion(() => Assert.False(session.IsOver));

        Assert.True(ChessSessionTests.Play(session, PieceColor.White, "e2", "e4")); // Bia (brancas agora)
        ChessDuoHarness.Button(a, "Abandonar").Click();
        ChessDuoHarness.Button(a, "Confirmar").Click();
        b.WaitForAssertion(() => Assert.Contains("Oponente abandonou. Vitória por W.O.", ChessDuoHarness.Text(b), StringComparison.Ordinal));
        ChessDuoHarness.Button(b, "Voltar ao lobby").Click();
        await _h.WaitForRowsAsync(2);

        var c = _h.Open("Cid");
        var d = _h.Open("Dan");
        foreach (var (cut, color) in new[] { (c, "Brancas"), (d, "Pretas") })
        {
            cut.FindAll("[role='radio']").First(r => r.TextContent.Contains("Blitz", StringComparison.Ordinal)).Click();
            cut.FindAll("[role='radio']").First(r => r.TextContent.Contains(color, StringComparison.Ordinal)).Click();
            ChessDuoHarness.Button(cut, "Procurar oponente").Click();
        }

        c.WaitForAssertion(() => Assert.NotEmpty(c.FindComponents<ChessArena>()));
        d.WaitForAssertion(() => Assert.NotEmpty(d.FindComponents<ChessArena>()));
        await ChessDuoHarness.CircuitAsync(_h.Contexts[3], up: false);
        c.WaitForAssertion(() => Assert.Contains("Oponente desconectado. Aguardando reconexão…", ChessDuoHarness.Text(c), StringComparison.Ordinal));
        _h.Time.Advance(TimeSpan.FromSeconds(15));
        c.WaitForAssertion(() => Assert.Contains("Oponente desconectou. Vitória por W.O.", ChessDuoHarness.Text(c), StringComparison.Ordinal), TimeSpan.FromSeconds(5));
        await _h.WaitForRowsAsync(3);

        await using var db = new GameplayDbContext(_h.Options);
        var page = await new GameResultService(db, NullLogger<GameResultService>.Instance)
            .GetHistoryAsync(new HistoryQuery(null, HistoryScope.All, HistoryFilter.All, null, HistorySort.Recent, 1, 10, GameType.Chess));
        Assert.Contains(page.Items, i => i.Reason == "Desconexão do oponente");
        Assert.Equal(3, page.TotalItems);
    }
}
