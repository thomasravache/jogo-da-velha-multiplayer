using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Services.Chess;

namespace TicTacToe.Tests;

// SPEC-0053: mapeamento ChessSession -> MatchResult e gravação única

public class ChessResultRecorderTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class ThrowingResultService(GameplayDbContext db) : GameResultService(db, NullLogger<GameResultService>.Instance)
    {
        public override Task SaveChessAsync(ChessMatchRecord record) => throw new InvalidOperationException("falha simulada");
    }

    private static DbContextOptions<GameplayDbContext> NewOptions() =>
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static (GameplayDbContext Db, ChessResultRecorder Recorder) Build(DbContextOptions<GameplayDbContext>? options = null)
    {
        var db = new GameplayDbContext(options ?? NewOptions());
        return (db, new ChessResultRecorder(new GameResultService(db, NullLogger<GameResultService>.Instance), NullLogger<ChessResultRecorder>.Instance));
    }

    [Fact(DisplayName = "SPEC-0053:IT-01 — Vitória por mate grava brancas como X, pretas como O, lances, FEN e duração")]
    [Trait("Category", "SPEC-0053:IT-01")]
    public async Task SaveOnce_ShouldRecordCheckmate()
    {
        var (db, recorder) = Build();
        await using var _ = db;
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        time.Advance(42 * Second);
        ChessSessionLeaveTests.FoolsMate(session);

        var saved = await recorder.SaveOnceAsync(session);

        Assert.True(saved);
        var row = await db.MatchResults.SingleAsync();
        Assert.Equal(GameType.Chess, row.GameType);
        Assert.Equal(("Ana", "Bia"), (row.PlayerXName, row.PlayerOName));
        Assert.Equal(session.GetPlayerId(PieceColor.White), row.PlayerXId);
        Assert.Equal(session.GetPlayerId(PieceColor.Black), row.PlayerOId);
        Assert.Equal("O", row.WinnerSide);
        Assert.Equal("Bia", row.WinnerName);
        Assert.Equal(EndReason.Checkmate, row.EndReason);
        Assert.Equal("blitz5+0", row.TimeControl);
        Assert.Equal("f3 e5 g4 Qh4#", row.MovesSan);
        Assert.Equal(session.Snapshot().Position.ToFen(), row.FinalFen);
        Assert.Equal(4, row.MoveCount);
        Assert.Equal(42, row.DurationSeconds);
        Assert.Equal(GameMode.Online, row.Mode);
    }

    [Fact(DisplayName = "SPEC-0053:IT-01 — Empate por afogamento grava sem vencedor")]
    [Trait("Category", "SPEC-0053:IT-01")]
    public async Task SaveOnce_ShouldRecordStalemateAsDraw()
    {
        var (db, recorder) = Build();
        await using var _ = db;
        using var session = ChessSessionTests.New(start: Position.FromFen("7k/8/5K2/8/8/8/8/6Q1 w - - 0 1"));
        ChessSessionTests.Line(session, "g1g6");

        Assert.True(await recorder.SaveOnceAsync(session));

        var row = await db.MatchResults.SingleAsync();
        Assert.Null(row.WinnerSide);
        Assert.Null(row.WinnerName);
        Assert.Equal(EndReason.Stalemate, row.EndReason);
        Assert.Equal(1, row.MoveCount);
    }

    [Fact(DisplayName = "SPEC-0053:IT-01 — Vitória por tempo grava Timeout com o vencedor")]
    [Trait("Category", "SPEC-0053:IT-01")]
    public async Task SaveOnce_ShouldRecordTimeout()
    {
        var (db, recorder) = Build();
        await using var _ = db;
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        ChessSessionTests.Line(session, "e2e4");
        time.Advance(TimeControl.Blitz.Initial + Second);
        session.Tick();

        Assert.True(await recorder.SaveOnceAsync(session));

        var row = await db.MatchResults.SingleAsync();
        Assert.Equal(EndReason.Timeout, row.EndReason);
        Assert.Equal("X", row.WinnerSide);
        Assert.Equal("Ana", row.WinnerName);
    }

    [Fact(DisplayName = "SPEC-0053:IT-01 — Abandono e desistência gravam Abandon")]
    [Trait("Category", "SPEC-0053:IT-01")]
    public async Task SaveOnce_ShouldRecordAbandonAndResignationAsAbandon()
    {
        var (db, recorder) = Build();
        await using var _ = db;
        using var left = ChessSessionTests.New();
        using var resigned = ChessSessionTests.New();
        left.Leave(PieceColor.White);
        resigned.Forfeit(PieceColor.Black, ChessEndReason.Resignation);

        Assert.True(await recorder.SaveOnceAsync(left));
        Assert.True(await recorder.SaveOnceAsync(resigned));

        var rows = await db.MatchResults.ToListAsync();
        Assert.All(rows, r => Assert.Equal(EndReason.Abandon, r.EndReason));
        Assert.Contains(rows, r => r.WinnerSide == "O");
        Assert.Contains(rows, r => r.WinnerSide == "X");
        Assert.All(rows, r => Assert.Equal(0, r.MoveCount));
    }

    [Fact(DisplayName = "SPEC-0053:IT-01 — Desconexão grava Disconnect")]
    [Trait("Category", "SPEC-0053:IT-01")]
    public async Task SaveOnce_ShouldRecordDisconnect()
    {
        var (db, recorder) = Build();
        await using var _ = db;
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        session.SetConnection(PieceColor.White, false);
        time.Advance((ChessSession.DisconnectGraceSeconds + 1) * Second);
        session.Tick();

        Assert.True(await recorder.SaveOnceAsync(session));

        var row = await db.MatchResults.SingleAsync();
        Assert.Equal(EndReason.Disconnect, row.EndReason);
        Assert.Equal("O", row.WinnerSide);
    }

    [Fact(DisplayName = "SPEC-0053:IT-01 — Partida solo grava com Mode Solo")]
    [Trait("Category", "SPEC-0053:IT-01")]
    public async Task SaveOnce_ShouldRecordSoloMode()
    {
        var (db, recorder) = Build();
        await using var _ = db;
        using var session = ChessSessionTests.New();
        session.Mode = ChessMode.Solo;
        ChessSessionLeaveTests.FoolsMate(session);

        Assert.True(await recorder.SaveOnceAsync(session));

        Assert.Equal(GameMode.Solo, (await db.MatchResults.SingleAsync()).Mode);
    }

    [Fact(DisplayName = "SPEC-0053:IT-01 — Partida em andamento não grava")]
    [Trait("Category", "SPEC-0053:IT-01")]
    public async Task SaveOnce_ShouldIgnoreRunningGame()
    {
        var (db, recorder) = Build();
        await using var _ = db;
        using var session = ChessSessionTests.New();

        Assert.False(await recorder.SaveOnceAsync(session));

        Assert.Empty(await db.MatchResults.ToListAsync());
    }

    [Fact(DisplayName = "SPEC-0053:IT-01 — A revanche zera a marca: grave antes de reiniciar")]
    [Trait("Category", "SPEC-0053:IT-01")]
    public async Task SaveOnce_BeforeRematch_ShouldRecordEachGameOnce()
    {
        var (db, recorder) = Build();
        await using var _ = db;
        using var session = ChessSessionTests.New();
        ChessSessionLeaveTests.FoolsMate(session);

        Assert.True(await recorder.SaveOnceAsync(session));
        Assert.True(session.RequestRematch(PieceColor.White));
        Assert.True(session.AcceptRematch(PieceColor.Black));
        ChessSessionLeaveTests.FoolsMate(session);
        Assert.True(await recorder.SaveOnceAsync(session));

        var rows = await db.MatchResults.OrderBy(r => r.PlayedAt).ToListAsync();
        Assert.Equal(2, rows.Count);
        // Após a troca de cores, as brancas são o segundo assento (Bia).
        Assert.Contains(rows, r => r.PlayerXName == "Bia" && r.PlayerOName == "Ana");
    }

    [Theory(DisplayName = "SPEC-0053:UT-04 — Motivos de encerramento mapeiam para o motivo gravado")]
    [Trait("Category", "SPEC-0053:UT-04")]
    [InlineData(ChessEndReason.Checkmate, EndReason.Checkmate)]
    [InlineData(ChessEndReason.Stalemate, EndReason.Stalemate)]
    [InlineData(ChessEndReason.InsufficientMaterial, EndReason.Insufficient)]
    [InlineData(ChessEndReason.FiftyMoveRule, EndReason.FiftyMoves)]
    [InlineData(ChessEndReason.ThreefoldRepetition, EndReason.Repetition)]
    [InlineData(ChessEndReason.Timeout, EndReason.Timeout)]
    [InlineData(ChessEndReason.Resignation, EndReason.Abandon)]
    [InlineData(ChessEndReason.Abandon, EndReason.Abandon)]
    [InlineData(ChessEndReason.Disconnect, EndReason.Disconnect)]
    public void MapReason_ShouldFollowContract(ChessEndReason reason, EndReason expected) =>
        Assert.Equal(expected, ChessResultRecorder.MapReason(reason));

    [Fact(DisplayName = "SPEC-0053:UT-04 — Todo ChessEndReason tem mapeamento e o texto gravado cabe em 16 caracteres")]
    [Trait("Category", "SPEC-0053:UT-04")]
    public void MapReason_ShouldCoverAllReasonsWithinColumnSize()
    {
        foreach (var reason in Enum.GetValues<ChessEndReason>())
        {
            Assert.True(ChessResultRecorder.MapReason(reason).ToString().Length <= 16, reason.ToString());
        }

        foreach (var reason in new[] { EndReason.Checkmate, EndReason.Stalemate, EndReason.Insufficient, EndReason.FiftyMoves, EndReason.Repetition })
        {
            Assert.True(reason.ToString().Length <= 16, reason.ToString());
        }
    }

    [Fact(DisplayName = "SPEC-0053:IT-03 — Chamadores concorrentes gravam uma única linha")]
    [Trait("Category", "SPEC-0053:IT-03")]
    public async Task SaveOnce_Concurrent_ShouldRecordOnce()
    {
        var options = NewOptions();
        using var session = ChessSessionTests.New();
        ChessSessionLeaveTests.FoolsMate(session);
        var gate = new ManualResetEventSlim();

        Task<bool> Caller() => Task.Run(async () =>
        {
            var (db, recorder) = Build(options);
            await using var _ = db;
            gate.Wait();
            return await recorder.SaveOnceAsync(session);
        });

        var callers = Enumerable.Range(0, 8).Select(_ => Caller()).ToArray();
        gate.Set();
        var results = await Task.WhenAll(callers);

        Assert.Equal(1, results.Count(r => r));
        await using var check = new GameplayDbContext(options);
        Assert.Single(await check.MatchResults.ToListAsync());
    }

    [Fact(DisplayName = "SPEC-0053:IT-03 — Falha de gravação é registrada em log e não propaga")]
    [Trait("Category", "SPEC-0053:IT-03")]
    public async Task SaveOnce_WhenSaveFails_ShouldLogAndNotThrow()
    {
        await using var db = new GameplayDbContext(NewOptions());
        var logger = new ListLogger<ChessResultRecorder>();
        var recorder = new ChessResultRecorder(new ThrowingResultService(db), logger);
        using var session = ChessSessionTests.New();
        ChessSessionLeaveTests.FoolsMate(session);

        var saved = await recorder.SaveOnceAsync(session);

        Assert.False(saved);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Empty(await db.MatchResults.ToListAsync());
    }
}
