using System.Text.Json;
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

// SPEC-0058: treino solo de xadrez contra o robô (cartão no lobby, fluxo da página, gravação e cancelamento)

public sealed class ChessSoloTests : IDisposable
{
    private readonly ChessMatchRegistry _registry = new();
    private static readonly TimeSpan BotDelay = TimeSpan.FromMilliseconds(600);

    private readonly ProbeTime _time = new(new ManualTime());
    private readonly MatchmakingService _matchmaking = new();
    private readonly DbContextOptions<GameplayDbContext> _options =
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private readonly List<BunitContext> _contexts = [];
    private readonly Guid _playerId = Guid.NewGuid();
    private int _flips;
    private int _fired;
    private Func<ChessBotLevel, IChessBot> _botFactory = level => ChessBots.Create(level);

    public void Dispose()
    {
        foreach (var ctx in _contexts)
        {
            ctx.Dispose();
        }
    }

    private bool CoinFlip() => Interlocked.Increment(ref _flips) % 2 == 1; // primeira chamada: brancas

    private IRenderedComponent<ChessHome> Open(string nick = "Ana")
    {
        var storage = new InMemoryPlayerStorage();
        storage.Data["xo.player"] = JsonSerializer.Serialize(new { id = _playerId, nick });
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(_matchmaking);
        ctx.Services.AddSingleton<Func<ChessBotLevel, IChessBot>>(level => _botFactory(level));
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
        var cut = ctx.Render<ChessHome>();
        cut.WaitForAssertion(() => Assert.Equal(nick, cut.Find("input#playerName").GetAttribute("value")));
        return cut;
    }

    private static void Choose(IRenderedComponent<ChessHome> cut, string text) =>
        cut.FindAll("[role='radio']").First(r => r.TextContent.Contains(text, StringComparison.Ordinal)).Click();

    private static void Click(IRenderedComponent<ChessHome> cut, string text) =>
        cut.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains(text, StringComparison.Ordinal)).Click();

    private IRenderedComponent<ChessHome> StartSolo(string level, string color, string? nick = "Ana")
    {
        var cut = Open(nick ?? "Ana");
        Choose(cut, level);
        Choose(cut, color);
        Click(cut, "Iniciar partida solo");
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<ChessArena>()));
        return cut;
    }

    private static ChessSession SessionOf(IRenderedComponent<ChessHome> cut) => cut.FindComponent<ChessArena>().Instance.Session;

    private static int MoveCount(ChessSession session) => session.Snapshot().Moves.Count;

    // Cada execução do robô cria exatamente um temporizador de 600 ms: espera ele existir, avança só esse atraso
    // (sem escoar o relógio da partida) e o resto é polling até o prazo.
    private void AdvanceUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "O robô não jogou no prazo.");
            var timers = _time.BotTimers;
            if (timers > _fired)
            {
                _fired = timers;
                _time.Advance(BotDelay);
            }

            Thread.Sleep(5);
        }
    }

    // Robô que trava na busca (ignora o token) até o teste liberar.
    private sealed class BlockingBot : IChessBot
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Name => "Trava";

        public void Release() => _release.TrySetResult();

        public async Task<Move?> ChooseMoveAsync(Position position, CancellationToken ct)
        {
            Started.TrySetResult();
            await _release.Task;
            return null;
        }
    }

    // Relógio manual que conta os atrasos do robô (temporizadores de 600 ms).
    private sealed class ProbeTime(ManualTime inner) : TimeProvider
    {
        private int _botTimers;

        public int BotTimers => Volatile.Read(ref _botTimers);

        public override DateTimeOffset GetUtcNow() => inner.GetUtcNow();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime == BotDelay)
            {
                Interlocked.Increment(ref _botTimers);
            }

            return inner.CreateTimer(callback, state, dueTime, period);
        }

        public void Advance(TimeSpan by) => inner.Advance(by);
    }

    private static async Task Invoke(IRenderedComponent<ChessHome> cut, string method)
    {
        await cut.InvokeAsync(async () =>
        {
            var result = typeof(ChessHome).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, null);
            if (result is Task task)
            {
                await task;
            }
        });
        cut.Render();
    }

    private static void HumanPlaysFirstLegal(ChessSession session, int humanSeat)
    {
        var color = session.ColorOf(humanSeat);
        var move = session.Snapshot().Position.LegalMoves()[0];
        Assert.True(session.TryMove(color, move.From, move.To, move.Promotion, out _));
    }

    private async Task<List<MatchResult>> RowsAsync()
    {
        await using var db = new GameplayDbContext(_options);
        return await db.MatchResults.ToListAsync();
    }

    private async Task WaitForRowsAsync(int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && (await RowsAsync()).Count < expected)
        {
            await Task.Delay(25);
        }
    }

    // ---------- UT-02: cartão de solo no lobby ----------

    [Fact(DisplayName = "SPEC-0058:UT-02 — radiogrupo de dificuldade marca o nível, mostra a descrição e notifica a escolha")]
    [Trait("Category", "SPEC-0058:UT-02")]
    public void Lobby_ShouldShowLevelRadioGroupAndDescription()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ChessBotLevel? chosen = null;
        var cut = ctx.Render<ChessLobby>(p => p
            .Add(c => c.PlayerName, "Ana")
            .Add(c => c.SelectedLevel, ChessBotLevel.Easy)
            .Add(c => c.LevelChanged, level => chosen = level));

        var group = cut.Find("[role='radiogroup'][aria-label='Dificuldade do robô']");
        var radios = group.QuerySelectorAll("[role='radio']");
        Assert.Equal(2, radios.Length);
        Assert.Contains("Fácil", radios[0].TextContent, StringComparison.Ordinal);
        Assert.Equal("true", radios[0].GetAttribute("aria-checked"));
        Assert.Contains("Médio", radios[1].TextContent, StringComparison.Ordinal);
        Assert.Equal("false", radios[1].GetAttribute("aria-checked"));
        var easyText = cut.Find("[data-level-description]").TextContent;
        Assert.False(string.IsNullOrWhiteSpace(easyText));

        radios[1].Click();
        Assert.Equal(ChessBotLevel.Medium, chosen);
        cut.Render(p => p.Add(c => c.SelectedLevel, ChessBotLevel.Medium));
        Assert.NotEqual(easyText, cut.Find("[data-level-description]").TextContent);
        Assert.Equal("true", cut.Find("[role='radiogroup'][aria-label='Dificuldade do robô'] [role='radio']:nth-child(2)").GetAttribute("aria-checked"));
    }

    [Fact(DisplayName = "SPEC-0058:UT-02 — Iniciar partida solo fica desabilitado sem apelido e dispara OnPlaySolo com apelido")]
    [Trait("Category", "SPEC-0058:UT-02")]
    public void Lobby_ShouldDisableSoloWithoutNickname()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var played = 0;
        var cut = ctx.Render<ChessLobby>(p => p
            .Add(c => c.PlayerName, "")
            .Add(c => c.OnPlaySolo, () => played++));

        var solo = cut.FindAll("button").First(b => b.TextContent.Contains("Iniciar partida solo", StringComparison.Ordinal));
        Assert.True(solo.HasAttribute("disabled"));

        cut.Render(p => p.Add(c => c.PlayerName, "Ana"));
        solo = cut.FindAll("button").First(b => b.TextContent.Contains("Iniciar partida solo", StringComparison.Ordinal));
        Assert.False(solo.HasAttribute("disabled"));
        solo.Click();
        Assert.Equal(1, played);
    }

    // ---------- UT-03: início da partida ----------

    [Fact(DisplayName = "SPEC-0058:UT-03 — humano de brancas: sessão solo, robô sem PlayerId no outro lado e humano abre")]
    [Trait("Category", "SPEC-0058:UT-03")]
    public void Start_ShouldCreateSoloSessionWithHumanWhite()
    {
        var cut = StartSolo("Médio", "Brancas");
        var session = SessionOf(cut);

        Assert.Equal(ChessMode.Solo, session.Mode);
        Assert.Equal(0, cut.FindComponent<ChessArena>().Instance.MySeat);
        Assert.Equal(PieceColor.White, session.ColorOf(0));
        Assert.Equal("Ana", session.GetSeatName(0));
        Assert.Equal(_playerId, session.GetSeatPlayerId(0));
        Assert.Equal(ChessBots.NameOf(ChessBotLevel.Medium), session.GetSeatName(1));
        Assert.Null(session.GetSeatPlayerId(1));
        Assert.Equal(PieceColor.Black, session.ColorOf(1));
        Assert.Equal(0, MoveCount(session));
    }

    [Fact(DisplayName = "SPEC-0058:UT-03 — humano de pretas: robô de brancas faz o primeiro lance")]
    [Trait("Category", "SPEC-0058:UT-03")]
    public void Start_ShouldLetBotOpenWhenHumanIsBlack()
    {
        var cut = StartSolo("Fácil", "Pretas");
        var session = SessionOf(cut);

        Assert.Equal(PieceColor.Black, session.ColorOf(0));
        Assert.Equal(ChessBots.NameOf(ChessBotLevel.Easy), session.GetSeatName(1));
        Assert.Null(session.GetSeatPlayerId(1));
        Assert.Equal(0, MoveCount(session));

        AdvanceUntil(() => MoveCount(session) == 1);

        Assert.Equal(PieceColor.Black, session.Snapshot().SideToMove);
    }

    [Fact(DisplayName = "SPEC-0058:UT-03 — Aleatória sorteia a cor do humano uma única vez")]
    [Trait("Category", "SPEC-0058:UT-03")]
    public void Start_ShouldFlipOnceForRandomColor()
    {
        var cut = StartSolo("Fácil", "Aleatória");

        Assert.Equal(1, _flips);
        Assert.Equal(PieceColor.White, SessionOf(cut).ColorOf(0)); // primeira chamada do sorteio: brancas
    }

    // ---------- UT-04: fluxo do robô ----------

    [Fact(DisplayName = "SPEC-0058:UT-04 — após e2→e4 o robô responde com lance legal depois do atraso; 'Vez de Robô' enquanto pensa")]
    [Trait("Category", "SPEC-0058:UT-04")]
    public void Flow_ShouldReplyAfterDelayWithLegalMove()
    {
        var cut = StartSolo("Fácil", "Brancas");
        var session = SessionOf(cut);

        Assert.True(ChessSessionTests.Play(session, PieceColor.White, "e2", "e4"));
        cut.WaitForAssertion(() => Assert.Contains("Vez de Robô", cut.Find("[data-notice='turn']").TextContent, StringComparison.Ordinal));
        Assert.Equal(1, MoveCount(session));

        AdvanceUntil(() => MoveCount(session) == 2);

        var snapshot = session.Snapshot();
        Assert.Equal(PieceColor.White, snapshot.SideToMove); // TryMove só aceita lance legal
        Assert.Equal(PieceColor.Black, snapshot.Position.PieceAt(snapshot.Moves[1].Move.To)!.Value.Color);
        cut.WaitForAssertion(() => Assert.Contains("Sua vez", cut.Find("[data-notice='turn']").TextContent, StringComparison.Ordinal));
    }

    // ---------- UT-05: fim e revanche ----------

    [Fact(DisplayName = "SPEC-0058:UT-05 — Jogar novamente reinicia na hora com cores trocadas e o robô abre de brancas")]
    [Trait("Category", "SPEC-0058:UT-05")]
    public async Task Rematch_ShouldSwapColorsAndLetBotOpen()
    {
        var cut = StartSolo("Fácil", "Brancas");
        var session = SessionOf(cut);
        Assert.True(session.Forfeit(PieceColor.White, ChessEndReason.Resignation));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-end-card]")));

        Click(cut, "Jogar novamente");
        await WaitForRowsAsync(1);

        Assert.Same(session, SessionOf(cut));
        Assert.False(session.IsOver);
        Assert.Equal(PieceColor.Black, session.ColorOf(0));
        Assert.Equal(PieceColor.White, session.ColorOf(1));
        Assert.Equal(0, MoveCount(session));
        Assert.Single(await RowsAsync()); // a partida encerrada foi gravada antes da revanche

        AdvanceUntil(() => MoveCount(session) == 1);
        Assert.Equal(PieceColor.Black, session.Snapshot().SideToMove);
    }

    // ---------- UT-06: abandono ----------

    [Fact(DisplayName = "SPEC-0058:UT-06 — Abandonar em partida solo em andamento volta ao lobby sem gravar e descarta a sessão")]
    [Trait("Category", "SPEC-0058:UT-06")]
    public async Task Abandon_ShouldDiscardWithoutRecording()
    {
        var cut = StartSolo("Fácil", "Brancas");
        var session = SessionOf(cut);
        Assert.True(ChessSessionTests.Play(session, PieceColor.White, "e2", "e4"));

        Click(cut, "Abandonar");

        Assert.NotNull(cut.Find("input#playerName"));
        Assert.Empty(cut.FindComponents<ChessArena>());
        Assert.True(session.HasLeft(PieceColor.White)); // Leave devolveu Discarded: sessão marcada como descartada
        _time.Advance(TimeSpan.FromSeconds(2)); // o robô cancelado não joga depois do descarte
        await Task.Delay(100);
        Assert.Equal(1, MoveCount(session));
        Assert.Empty(await RowsAsync());
    }

    // ---------- IT-01: gravação ----------

    [Fact(DisplayName = "SPEC-0058:IT-01 — partida solo encerrada grava uma linha Mode=Solo, no histórico e fora do ranking")]
    [Trait("Category", "SPEC-0058:IT-01")]
    public async Task Finish_ShouldRecordSoloOutsideLeaderboard()
    {
        var cut = StartSolo("Fácil", "Brancas");
        var session = SessionOf(cut);
        ChessSessionTests.Line(session, "f2f3");
        AdvanceUntil(() => MoveCount(session) == 2);
        Assert.True(session.Forfeit(PieceColor.White, ChessEndReason.Resignation));

        await WaitForRowsAsync(1);
        var row = Assert.Single(await RowsAsync());
        Assert.Equal(GameMode.Solo, row.Mode);
        Assert.Equal(GameType.Chess, row.GameType);
        Assert.Equal("Ana", row.PlayerXName);
        Assert.Equal(ChessBots.NameOf(ChessBotLevel.Easy), row.PlayerOName);

        await using var db = new GameplayDbContext(_options);
        var service = new GameResultService(db, NullLogger<GameResultService>.Instance);
        var history = await service.GetHistoryAsync(new HistoryQuery(_playerId, HistoryScope.Mine, HistoryFilter.All, null, HistorySort.Recent, 1, 10, GameType.Chess));
        Assert.Equal(1, history.TotalItems);
        var board = await service.GetLeaderboardPageAsync(new LeaderboardQuery(null, 1, 10, GameType.Chess));
        Assert.Empty(board.Items);
    }

    // ---------- IT-02: cancelamento ----------

    [Fact(DisplayName = "SPEC-0058:IT-02 — sair da página durante o atraso do robô: nenhum lance depois e sem exceção")]
    [Trait("Category", "SPEC-0058:IT-02")]
    public async Task Dispose_ShouldCancelPendingBotMove()
    {
        var cut = StartSolo("Fácil", "Pretas"); // o robô (brancas) abriria após o atraso
        var session = SessionOf(cut);
        Assert.Equal(0, MoveCount(session));

        await _contexts[^1].DisposeComponentsAsync(); // sai da página
        _time.Advance(TimeSpan.FromSeconds(2));
        await Task.Delay(150);

        Assert.Equal(0, MoveCount(session));
    }

    // ---------- E2E-01: jornada ----------

    [Fact(DisplayName = "SPEC-0058:E2E-01 — Médio de brancas: três lances, abandonar sem gravar; nova partida encerrada é gravada como solo")]
    [Trait("Category", "SPEC-0058:E2E-01")]
    public async Task Journey_ShouldPlayAbandonAndRecord()
    {
        var cut = StartSolo("Médio", "Brancas");
        var session = SessionOf(cut);
        for (var i = 1; i <= 3; i++)
        {
            HumanPlaysFirstLegal(session, 0);
            AdvanceUntil(() => MoveCount(session) == i * 2);
        }

        Click(cut, "Abandonar");
        // O re-render pós-Leave é assíncrono (LeaveMatchCore agenda StateHasChanged no dispatcher): espera o lobby, sem Find imediato.
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("input#playerName")), TimeSpan.FromSeconds(30));
        Assert.Empty(await RowsAsync());

        Choose(cut, "Médio");
        Choose(cut, "Brancas");
        Click(cut, "Iniciar partida solo");
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<ChessArena>()), TimeSpan.FromSeconds(30));
        var second = SessionOf(cut);
        Assert.NotSame(session, second);
        Assert.True(second.Forfeit(PieceColor.White, ChessEndReason.Resignation));

        await WaitForRowsAsync(1);
        var row = Assert.Single(await RowsAsync());
        Assert.Equal(GameMode.Solo, row.Mode);
        await using var db = new GameplayDbContext(_options);
        var service = new GameResultService(db, NullLogger<GameResultService>.Instance);
        var history = await service.GetHistoryAsync(new HistoryQuery(null, HistoryScope.All, HistoryFilter.All, null, HistorySort.Recent, 1, 10, GameType.Chess));
        Assert.Equal(1, history.TotalItems);
    }

    // ---------- Revisão G4 ----------

    [Fact(DisplayName = "SPEC-0058:IT-02 — abandonar durante a busca e iniciar novo solo de pretas: o robô de brancas abre")]
    [Trait("Category", "SPEC-0058:IT-02")]
    public void NewSolo_ShouldNotInheritBusyBotFromAbandonedMatch()
    {
        var stuck = new BlockingBot();
        var calls = 0;
        _botFactory = level => Interlocked.Increment(ref calls) == 1 ? stuck : ChessBots.Create(level);
        var cut = StartSolo("Fácil", "Pretas");
        AdvanceUntil(() => stuck.Started.Task.IsCompleted); // o primeiro robô está preso na busca

        Click(cut, "Abandonar");
        Choose(cut, "Pretas");
        Click(cut, "Iniciar partida solo");
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<ChessArena>()));
        var second = SessionOf(cut);

        AdvanceUntil(() => MoveCount(second) == 1);

        Assert.Equal(PieceColor.Black, second.Snapshot().SideToMove);
        stuck.Release();
    }

    [Fact(DisplayName = "SPEC-0058:UT-06 — Abandonar quando a bandeira caiu no clique grava a partida encerrada")]
    [Trait("Category", "SPEC-0058:UT-06")]
    public async Task Abandon_ShouldRecordWhenFlagFellAtClick()
    {
        var stuck = new BlockingBot();
        _botFactory = _ => stuck;
        var cut = StartSolo("Fácil", "Brancas");
        var session = SessionOf(cut);
        Assert.True(ChessSessionTests.Play(session, PieceColor.White, "e2", "e4"));
        AdvanceUntil(() => stuck.Started.Task.IsCompleted);
        _time.Advance(TimeSpan.FromMilliseconds(500)); // consome o pulso de 1 s da sessão antes de a bandeira cair
        _time.Advance(TimeSpan.FromMinutes(6)); // a bandeira das pretas cai sem nenhum aviso da sessão

        await Invoke(cut, "AbandonSolo");

        Assert.NotNull(cut.Find("input#playerName"));
        await WaitForRowsAsync(1);
        var row = Assert.Single(await RowsAsync());
        Assert.Equal(GameMode.Solo, row.Mode);
        Assert.Equal(EndReason.Timeout, row.EndReason);
        stuck.Release();
    }

    [Fact(DisplayName = "SPEC-0058:UT-02 — descrição da cor informa que contra o robô a cor escolhida sempre vale")]
    [Trait("Category", "SPEC-0058:UT-02")]
    public void Lobby_ShouldExplainColorInSolo()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<ChessLobby>(p => p.Add(c => c.PlayerName, "Ana").Add(c => c.SelectedColor, ColorPreference.White));

        Assert.Contains("Contra o robô, sua cor sempre vale", cut.Find("[data-color-description]").TextContent, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0058:UT-03 — iniciar solo cancela a busca de oponente pendente")]
    [Trait("Category", "SPEC-0058:UT-03")]
    public void StartSolo_ShouldCancelPendingQueue()
    {
        var a = Open("Ana");
        Click(a, "Procurar oponente");
        Click(a, "Iniciar partida solo");
        a.WaitForAssertion(() => Assert.NotEmpty(a.FindComponents<ChessArena>()));

        var b = Open("Bia");
        Click(b, "Procurar oponente");

        Assert.Empty(b.FindComponents<ChessArena>()); // não pareou com a fila fantasma da Ana
        Assert.Contains("Na fila", b.Markup, StringComparison.Ordinal);
        Assert.Equal(ChessMode.Solo, SessionOf(a).Mode);
    }

    [Fact(DisplayName = "SPEC-0058:UT-03 — iniciar solo cancela a sala privada pendente")]
    [Trait("Category", "SPEC-0058:UT-03")]
    public void StartSolo_ShouldCancelPendingRoom()
    {
        var a = Open("Ana");
        Click(a, "Criar sala");
        var code = a.Find("[aria-label='Código da sala']").TextContent.Trim();
        Click(a, "Iniciar partida solo");
        a.WaitForAssertion(() => Assert.NotEmpty(a.FindComponents<ChessArena>()));

        var b = Open("Bia");
        Choose(b, "Entrar com código");
        b.Find("input#roomCode").Input(code);
        Click(b, "Entrar");

        Assert.Empty(b.FindComponents<ChessArena>());
        Assert.Contains("Sala inválida ou já iniciada!", b.Markup, StringComparison.Ordinal);
    }
}
