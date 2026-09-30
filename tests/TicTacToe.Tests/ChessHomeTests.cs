using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
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
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0056: página /xadrez (pareamento, sessão única, cores, gravação, imersão e descarte)

public sealed class ChessHomeTests : IDisposable
{
    private readonly MatchmakingService _matchmaking = new();
    private readonly ChessMatchRegistry _registry = new();
    private readonly ManualTime _time = new();
    private readonly DbContextOptions<GameplayDbContext> _options =
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private readonly List<BunitContext> _contexts = [];
    private int _flips;

    /// <summary>Cara-ou-coroa de teste: alterna a cada chamada (instável de propósito).</summary>
    private bool CoinFlip() => Interlocked.Increment(ref _flips) % 2 == 1;

    public void Dispose()
    {
        foreach (var ctx in _contexts)
        {
            ctx.Dispose();
        }
    }

    /// <summary>Um circuito: contexto próprio (identidade, shell) sobre os serviços singleton compartilhados.</summary>
    private BunitContext NewCircuit(string nick)
    {
        var storage = new InMemoryPlayerStorage();
        storage.Data["xo.player"] = JsonSerializer.Serialize(new { id = Guid.NewGuid(), nick });
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(_matchmaking);
        ctx.Services.AddSingleton(_registry);
        ctx.Services.AddSingleton<TimeProvider>(_time);
        ctx.Services.AddSingleton<Func<bool>>(CoinFlip);
        ctx.Services.AddSingleton(new ShellState());
        ctx.Services.AddSingleton<IPlayerStorage>(storage);
        ctx.Services.AddScoped<PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(_options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        ctx.Services.AddScoped(sp => new ChessResultRecorder(sp.GetRequiredService<GameResultService>(), NullLogger<ChessResultRecorder>.Instance));
        _contexts.Add(ctx);
        return ctx;
    }

    private static IRenderedComponent<ChessHome> Open(BunitContext ctx, string nick)
    {
        var cut = ctx.Render<ChessHome>();
        cut.WaitForAssertion(() => Assert.Equal(nick, cut.Find("input#playerName").GetAttribute("value")));
        return cut;
    }

    private static IElement Button(IRenderedComponent<ChessHome> cut, string text) =>
        cut.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains(text, StringComparison.Ordinal));

    private static void Choose(IRenderedComponent<ChessHome> cut, string text) =>
        cut.FindAll("[role='radio']").First(r => r.TextContent.Contains(text, StringComparison.Ordinal)).Click();

    private static void Search(IRenderedComponent<ChessHome> cut, string control, string color)
    {
        Choose(cut, control);
        Choose(cut, color);
        Button(cut, "Procurar oponente").Click();
    }

    private static ChessSession SessionOf(IRenderedComponent<ChessHome> cut) =>
        cut.FindComponent<ChessArena>().Instance.Session;

    private static int SeatOf(IRenderedComponent<ChessHome> cut) =>
        cut.FindComponent<ChessArena>().Instance.MySeat;

    private static void WaitForArena(IRenderedComponent<ChessHome> cut) =>
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<ChessArena>()));

    private static string MyColor(IRenderedComponent<ChessHome> cut) =>
        SessionOf(cut).ColorOf(SeatOf(cut)).ToString();

    private async Task<List<MatchResult>> RowsAsync()
    {
        await using var db = new GameplayDbContext(_options);
        return await db.MatchResults.ToListAsync();
    }

    private async Task WaitForHistoryAsync(int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            await using var db = new GameplayDbContext(_options);
            var service = new GameResultService(db, NullLogger<GameResultService>.Instance);
            var page = await service.GetHistoryAsync(new HistoryQuery(null, HistoryScope.All, HistoryFilter.All, null, HistorySort.Recent, 1, 10, GameType.Chess));
            if (page.TotalItems >= expected)
            {
                return;
            }

            await Task.Delay(25);
        }
    }

    private static string ConnOf(IRenderedComponent<ChessHome> cut) =>
        (string)typeof(ChessHome).GetField("_connectionId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(cut.Instance)!;

    // Chama o manipulador como o evento de UI faria e força a renderização que o Blazor faria depois dele.
    private static async Task Call(IRenderedComponent<ChessHome> cut, string method)
    {
        await cut.InvokeAsync(() => typeof(ChessHome).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, null));
        cut.Render();
    }

    private (IRenderedComponent<ChessHome> A, IRenderedComponent<ChessHome> B) Pair(string colorA, string colorB, string control = "Blitz")
    {
        var a = Open(NewCircuit("Ana"), "Ana");
        var b = Open(NewCircuit("Bia"), "Bia");
        Search(a, control, colorA);
        Search(b, control, colorB);
        WaitForArena(a);
        WaitForArena(b);
        return (a, b);
    }

    // ---------- IT-01 ----------

    [Fact(DisplayName = "SPEC-0056:IT-01 — Brancas × Aleatória: sessão única, cores opostas e sem sorteio")]
    [Trait("Category", "SPEC-0056:IT-01")]
    public void Pair_ShouldHonorPreferenceWithoutCoinFlip()
    {
        var (a, b) = Pair("Brancas", "Aleatória");

        Assert.Same(SessionOf(a), SessionOf(b));
        Assert.Equal(TimeControl.Blitz, SessionOf(a).Control);
        Assert.Equal(ChessMode.Online, SessionOf(a).Mode);
        Assert.Equal("White", MyColor(a));
        Assert.Equal("Black", MyColor(b));
        Assert.NotEqual(SeatOf(a), SeatOf(b));
        Assert.Equal(0, _flips);
        Assert.Equal("Ana", SessionOf(a).GetPlayerName(PieceColor.White));
        Assert.Equal("Bia", SessionOf(a).GetPlayerName(PieceColor.Black));
        Assert.NotNull(SessionOf(a).GetPlayerId(PieceColor.White));
        Assert.Contains("Você", a.Find("[data-player-card='white']").TextContent, StringComparison.Ordinal);
        Assert.Contains("Você", b.Find("[data-player-card='black']").TextContent, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0056:IT-01 — Pretas × Pretas: sorteio uma única vez e cores opostas")]
    [Trait("Category", "SPEC-0056:IT-01")]
    public void Pair_ShouldFlipOnceWhenPreferencesConflict()
    {
        var (a, b) = Pair("Pretas", "Pretas");

        Assert.Same(SessionOf(a), SessionOf(b));
        Assert.Equal(1, _flips);
        Assert.NotEqual(MyColor(a), MyColor(b));
    }

    [Fact(DisplayName = "SPEC-0056:IT-01 — Aleatória × Aleatória sorteia uma vez; controle vem da fila")]
    [Trait("Category", "SPEC-0056:IT-01")]
    public void Pair_ShouldUseQueueControlAndFlipOnceForRandom()
    {
        var (a, b) = Pair("Aleatória", "Aleatória", "Bullet");

        Assert.Equal(TimeControl.Bullet, SessionOf(a).Control);
        Assert.Equal(1, _flips);
        Assert.NotEqual(MyColor(a), MyColor(b));
    }

    [Fact(DisplayName = "SPEC-0056:IT-01 — Controles diferentes não pareiam")]
    [Trait("Category", "SPEC-0056:IT-01")]
    public void DifferentControls_ShouldNotPair()
    {
        var a = Open(NewCircuit("Ana"), "Ana");
        var b = Open(NewCircuit("Bia"), "Bia");

        Search(a, "Bullet", "Brancas");
        Search(b, "Rápida", "Pretas");

        Assert.Empty(a.FindComponents<ChessArena>());
        Assert.Empty(b.FindComponents<ChessArena>());
        Assert.Contains("Na fila", b.Markup, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0056:IT-01 — Sala privada: convidado entra com o código e a partida é privada")]
    [Trait("Category", "SPEC-0056:IT-01")]
    public void PrivateRoom_ShouldPairHostAndGuest()
    {
        var a = Open(NewCircuit("Ana"), "Ana");
        var b = Open(NewCircuit("Bia"), "Bia");
        Choose(a, "Rápida");
        Choose(a, "Brancas");
        Button(a, "Criar sala").Click();
        var code = a.Find("[aria-label='Código da sala']").TextContent.Trim();
        Assert.StartsWith("SALA-", code, StringComparison.Ordinal);

        Choose(b, "Entrar com código");
        b.Find("input#roomCode").Input(code.ToLowerInvariant());
        Button(b, "Entrar").Click();
        WaitForArena(a);
        WaitForArena(b);

        Assert.Same(SessionOf(a), SessionOf(b));
        Assert.Equal(ChessMode.Private, SessionOf(a).Mode);
        Assert.Equal(TimeControl.Rapid, SessionOf(a).Control);
        Assert.Equal("White", MyColor(a));
        Assert.Equal("Black", MyColor(b));
    }

    // ---------- IT-02 ----------

    [Fact(DisplayName = "SPEC-0056:IT-02 — Fim da partida grava uma única linha e ambos voltam ao lobby")]
    [Trait("Category", "SPEC-0056:IT-02")]
    public async Task Finish_ShouldRecordOnceAndReturnToLobby()
    {
        var (a, b) = Pair("Brancas", "Pretas");
        ChessSessionTests.Line(SessionOf(a), "f2f3", "e7e5", "g2g4", "d8h4");

        a.WaitForAssertion(() => Assert.NotEmpty(a.FindAll("[data-end-card]")));
        b.WaitForAssertion(() => Assert.NotEmpty(b.FindAll("[data-end-card]")));
        await WaitForHistoryAsync(1);
        var row = Assert.Single(await RowsAsync());
        Assert.Equal(GameType.Chess, row.GameType);
        Assert.Equal("blitz5+0", row.TimeControl);
        Assert.Equal("f3 e5 g4 Qh4#", row.MovesSan);
        Assert.Equal("Ana", row.PlayerXName);
        Assert.Equal("Bia", row.PlayerOName);

        Button(a, "Voltar ao lobby").Click();
        Button(b, "Voltar ao lobby").Click();
        Assert.NotNull(a.Find("input#playerName"));
        Assert.NotNull(b.Find("input#playerName"));
        Assert.Empty(a.FindComponents<ChessArena>());
        Assert.Single(await RowsAsync());
    }

    // ---------- IT-03 ----------

    [Fact(DisplayName = "SPEC-0056:IT-03 — Cancelar a busca ou sair da página tira da fila: terceiro não pareia com fantasma")]
    [Trait("Category", "SPEC-0056:IT-03")]
    public async Task CancelledOrDisposedSearch_ShouldNotPairWithGhost()
    {
        var ctxA = NewCircuit("Ana");
        var ctxB = NewCircuit("Bia");
        var a = Open(ctxA, "Ana");
        var b = Open(ctxB, "Bia");
        Search(a, "Blitz", "Brancas");
        Assert.Contains("Na fila", a.Markup, StringComparison.Ordinal);
        Button(a, "Cancelar busca").Click(); // A cancela
        Assert.DoesNotContain("Na fila", a.Markup, StringComparison.Ordinal);

        Search(b, "Blitz", "Brancas");
        Assert.Contains("Na fila", b.Markup, StringComparison.Ordinal);
        await ctxB.DisposeComponentsAsync(); // B sai da página na fila

        var c = Open(NewCircuit("Caio"), "Caio");
        Search(c, "Blitz", "Pretas");

        Assert.Empty(c.FindComponents<ChessArena>());
        Assert.Contains("Na fila", c.Markup, StringComparison.Ordinal);
        Assert.Empty(a.FindComponents<ChessArena>());
    }

    [Fact(DisplayName = "SPEC-0056:IT-03 — Sala privada cancelada (ou página descartada) deixa de aceitar entrada")]
    [Trait("Category", "SPEC-0056:IT-03")]
    public async Task CancelledRoom_ShouldRejectJoin()
    {
        var ctxA = NewCircuit("Ana");
        var a = Open(ctxA, "Ana");
        Button(a, "Criar sala").Click();
        var code1 = a.Find("[aria-label='Código da sala']").TextContent.Trim();
        Button(a, "Cancelar sala").Click();
        Assert.Empty(a.FindAll("[aria-label='Código da sala']"));

        Button(a, "Criar sala").Click();
        var code2 = a.Find("[aria-label='Código da sala']").TextContent.Trim();
        await ctxA.DisposeComponentsAsync();

        var b = Open(NewCircuit("Bia"), "Bia");
        Choose(b, "Entrar com código");
        foreach (var code in new[] { code1, code2 })
        {
            b.Find("input#roomCode").Input(code);
            Button(b, "Entrar").Click();
            Assert.Contains("Sala inválida ou já iniciada!", b.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Empty(b.FindComponents<ChessArena>());
        }
    }


    // ---------- exclusividade fila x sala ----------

    [Fact(DisplayName = "SPEC-0056:IT-03 — Na fila, criar sala cancela a fila: sem fila fantasma")]
    [Trait("Category", "SPEC-0056:IT-03")]
    public async Task CreateRoomWhileQueued_ShouldLeaveQueue()
    {
        var a = Open(NewCircuit("Ana"), "Ana");
        Search(a, "Blitz", "Brancas");
        Assert.Contains("Na fila", a.Markup, StringComparison.Ordinal);

        await Call(a, "CreateRoom");

        Assert.DoesNotContain("Na fila", a.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(a.FindAll("[aria-label='Código da sala']"));
        var b = Open(NewCircuit("Bia"), "Bia");
        Search(b, "Blitz", "Pretas");
        Assert.Empty(b.FindComponents<ChessArena>());
        Assert.Empty(a.FindComponents<ChessArena>());
    }

    [Fact(DisplayName = "SPEC-0056:IT-03 — Na fila, entrar em sala inválida tira da fila e a tela reflete isso")]
    [Trait("Category", "SPEC-0056:IT-03")]
    public async Task InvalidJoinWhileQueued_ShouldLeaveQueueAndUpdateScreen()
    {
        var a = Open(NewCircuit("Ana"), "Ana");
        Search(a, "Blitz", "Brancas");
        Choose(a, "Entrar com código");
        a.Find("input#roomCode").Input("SALA-ZZZZ");

        await Call(a, "JoinRoom");

        Assert.DoesNotContain("Na fila", a.Markup, StringComparison.Ordinal);
        Assert.Contains("Sala inválida ou já iniciada!", a.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        var b = Open(NewCircuit("Bia"), "Bia");
        Search(b, "Blitz", "Pretas");
        Assert.Empty(b.FindComponents<ChessArena>());
    }

    [Fact(DisplayName = "SPEC-0056:IT-03 — Com sala criada, procurar oponente cancela a sala")]
    [Trait("Category", "SPEC-0056:IT-03")]
    public async Task FindMatchWhileHosting_ShouldCancelRoom()
    {
        var a = Open(NewCircuit("Ana"), "Ana");
        Button(a, "Criar sala").Click();
        var code = a.Find("[aria-label='Código da sala']").TextContent.Trim();

        await Call(a, "FindMatch");

        Assert.Empty(a.FindAll("[aria-label='Código da sala']"));
        Assert.Contains("Na fila", a.Markup, StringComparison.Ordinal);
        Assert.Null(_matchmaking.JoinPrivateRoom(code, "intruso", "Intruso", null, "xadrez"));
    }

    // ---------- corrida com o descarte ----------

    [Fact(DisplayName = "SPEC-0056:IT-03 — Pareamento entregue a página já descartada não cria partida nem assento")]
    [Trait("Category", "SPEC-0056:IT-03")]
    public async Task MatchDeliveredAfterDispose_ShouldCreateNothing()
    {
        var ctx = NewCircuit("Ana");
        var a = Open(ctx, "Ana");
        var connA = ConnOf(a);
        var stale = (Action<string, Guid>)typeof(MatchmakingService)
            .GetField("OnPlayerMatched", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(_matchmaking)!;
        await ctx.DisposeComponentsAsync();
        _matchmaking.JoinQueue(connA, "Ana", null, queueKey: "xadrez:blitz5+0");
        var id = _matchmaking.JoinQueue("raw", "Raw", null, queueKey: "xadrez:blitz5+0")!.Value;

        stale(connA, id);

        Assert.False(_registry.TryGet(id, out _));
    }

    [Fact(DisplayName = "SPEC-0056:IT-03 — Parceiro descartado antes do pareamento não segura a sessão: ao sair o outro, ela é removida")]
    [Trait("Category", "SPEC-0056:IT-03")]
    public async Task DeadOpponent_ShouldNotLeakSession()
    {
        var ctxA = NewCircuit("Ana");
        var a = Open(ctxA, "Ana");
        var connA = ConnOf(a);
        await ctxA.DisposeComponentsAsync();
        var b = Open(NewCircuit("Bia"), "Bia");
        var connB = ConnOf(b);
        _matchmaking.JoinQueue(connA, "Ana", null, queueKey: "xadrez:blitz5+0");
        var id = _matchmaking.JoinQueue(connB, "Bia", null, queueKey: "xadrez:blitz5+0")!.Value;
        WaitForArena(b);
        var session = SessionOf(b);
        Assert.True(session.Forfeit(session.ColorOf(1 - SeatOf(b)), ChessEndReason.Resignation));
        b.WaitForAssertion(() => Button(b, "Voltar ao lobby"));

        Button(b, "Voltar ao lobby").Click();

        Assert.False(_registry.TryGet(id, out _));
    }

    // ---------- UT-06 ----------

    [Fact(DisplayName = "SPEC-0056:UT-06 — Shell imersivo na partida e restaurado ao voltar ao lobby e ao descartar")]
    [Trait("Category", "SPEC-0056:UT-06")]
    public async Task Shell_ShouldBeImmersiveDuringMatchAndRestored()
    {
        var (a, b) = Pair("Brancas", "Pretas");
        var shellA = _contexts[0].Services.GetRequiredService<ShellState>();
        var shellB = _contexts[1].Services.GetRequiredService<ShellState>();

        a.WaitForAssertion(() => Assert.True(shellA.Immersive));
        Assert.Equal("Partida de xadrez", shellA.Title);
        b.WaitForAssertion(() => Assert.True(shellB.Immersive));

        ChessSessionTests.Line(SessionOf(a), "f2f3", "e7e5", "g2g4", "d8h4");
        a.WaitForAssertion(() => Button(a, "Voltar ao lobby"));
        Button(a, "Voltar ao lobby").Click();
        Assert.False(shellA.Immersive);
        Assert.Null(shellA.Title);

        await _contexts[1].DisposeComponentsAsync();
        Assert.False(shellB.Immersive);
    }

    [Fact(DisplayName = "SPEC-0056:UT-07 — Página tem o título 'Xadrez · XO Arena' e a rota /xadrez")]
    [Trait("Category", "SPEC-0056:UT-07")]
    public void Page_ShouldDeclareRouteAndTitle()
    {
        var route = typeof(ChessHome).GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), false)
            .Cast<Microsoft.AspNetCore.Components.RouteAttribute>().Single();
        Assert.Equal("/xadrez", route.Template);

        var razor = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../../src/TicTacToe/TicTacToe.Web/Components/Pages/ChessHome.razor")));
        Assert.Contains("<PageTitle>Xadrez · XO Arena</PageTitle>", razor, StringComparison.Ordinal);
        Assert.Contains("@rendermode InteractiveServer", razor, StringComparison.Ordinal);
    }

    // ---------- E2E-01 ----------

    private static void Click(IRenderedComponent<ChessHome> cut, string square) =>
        cut.Find($"button[data-square='{square}']").Click();

    [Fact(DisplayName = "SPEC-0056:E2E-01 — Jornada: Blitz e Aleatória, procurar, mate do pastor até o cartão de fim e partida gravada")]
    [Trait("Category", "SPEC-0056:E2E-01")]
    public async Task Journey_ShouldPlayScholarsMateAndPersist()
    {
        var (a, b) = Pair("Aleatória", "Aleatória");
        var (white, black) = MyColor(a) == "White" ? (a, b) : (b, a);

        (IRenderedComponent<ChessHome> Who, string From, string To)[] moves =
        [
            (white, "e2", "e4"), (black, "e7", "e5"), (white, "f1", "c4"), (black, "b8", "c6"),
            (white, "d1", "h5"), (black, "g8", "f6"), (white, "h5", "f7"),
        ];
        foreach (var (who, from, to) in moves)
        {
            who.WaitForAssertion(() => Assert.Contains("Sua vez", who.Find("[data-notice='turn']").TextContent, StringComparison.Ordinal));
            Click(who, from);
            Click(who, to);
        }

        a.WaitForAssertion(() => Assert.Contains("Xeque-mate", a.Find("[data-end-card]").TextContent, StringComparison.Ordinal));
        b.WaitForAssertion(() => Assert.Contains("Xeque-mate", b.Find("[data-end-card]").TextContent, StringComparison.Ordinal));
        await WaitForHistoryAsync(1);

        await using var db = new GameplayDbContext(_options);
        var service = new GameResultService(db, NullLogger<GameResultService>.Instance);
        var page = await service.GetHistoryAsync(new HistoryQuery(null, HistoryScope.All, HistoryFilter.All, null, HistorySort.Recent, 1, 10, GameType.Chess));
        Assert.Equal(1, page.TotalItems);
    }
}
