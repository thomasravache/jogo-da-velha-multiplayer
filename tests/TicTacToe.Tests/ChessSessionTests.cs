using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessSessionTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    internal static ChessSession New(ManualTime? time = null, TimeControl? control = null, Position? start = null)
    {
        var session = new ChessSession(control ?? TimeControl.Blitz, time ?? new ManualTime(), enableBackgroundTimer: false, start: start);
        session.SetSeat(0, "Ana", Guid.NewGuid(), PieceColor.White);
        session.SetSeat(1, "Bia", Guid.NewGuid(), PieceColor.Black);
        return session;
    }

    internal static bool Play(ChessSession session, PieceColor player, string from, string to, PieceType? promotion = null) =>
        session.TryMove(player, Square.Parse(from), Square.Parse(to), promotion, out _);

    private static void AssertSameState(ChessSnapshot expected, ChessSnapshot actual)
    {
        Assert.Equal(expected.Position.ToFen(), actual.Position.ToFen());
        Assert.Equal(expected.Moves, actual.Moves);
        Assert.Equal(expected.CapturedByWhite, actual.CapturedByWhite);
        Assert.Equal(expected.CapturedByBlack, actual.CapturedByBlack);
        Assert.Equal(expected.WhiteRemaining, actual.WhiteRemaining);
        Assert.Equal(expected.BlackRemaining, actual.BlackRemaining);
        Assert.Equal(expected.ClockRunning, actual.ClockRunning);
        Assert.Equal(expected.Result, actual.Result);
        Assert.Equal(expected.EndedAtUtc, actual.EndedAtUtc);
    }

    internal static void Line(ChessSession session, params string[] moves)
    {
        foreach (var move in moves)
        {
            var player = session.Snapshot().SideToMove;
            Assert.True(Play(session, player, move[..2], move[2..4]), move);
        }
    }

    [Fact(DisplayName = "SPEC-0052:UT-01 — assentos, cores e identidades coerentes")]
    [Trait("Category", "SPEC-0052:UT-01")]
    public void Seats_ShouldBeConsistent()
    {
        using var session = new ChessSession(TimeControl.Blitz, new ManualTime(), enableBackgroundTimer: false);
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        session.SetSeat(0, "Ana", idA, PieceColor.White);
        session.SetSeat(1, "Bia", idB, PieceColor.Black);

        Assert.Equal(PieceColor.White, session.ColorOf(0));
        Assert.Equal(PieceColor.Black, session.ColorOf(1));
        Assert.Equal(0, session.SeatOf(PieceColor.White));
        Assert.Equal(1, session.SeatOf(PieceColor.Black));
        Assert.Equal("Ana", session.GetSeatName(0));
        Assert.Equal("Bia", session.GetSeatName(1));
        Assert.Equal(idA, session.GetSeatPlayerId(0));
        Assert.Equal("Bia", session.GetPlayerName(PieceColor.Black));
        Assert.Equal(idA, session.GetPlayerId(PieceColor.White));
        Assert.Equal(idB, session.GetPlayerId(PieceColor.Black));
        Assert.Throws<InvalidOperationException>(() => session.SetSeat(0, "Ana", idA, PieceColor.Black));
        Assert.Equal(PieceColor.White, session.ColorOf(0));
    }

    [Fact(DisplayName = "SPEC-0052:UT-01 — assento sem identidade e cor trocada pelo assento")]
    [Trait("Category", "SPEC-0052:UT-01")]
    public void Seats_WithoutPlayerId_AndSwappedColors()
    {
        using var session = new ChessSession(TimeControl.Blitz, new ManualTime(), enableBackgroundTimer: false);

        session.SetSeat(0, "Robô", null, PieceColor.Black);
        session.SetSeat(1, "Bia", Guid.NewGuid(), PieceColor.White);

        Assert.Null(session.GetSeatPlayerId(0));
        Assert.Null(session.GetPlayerId(PieceColor.Black));
        Assert.Equal("Robô", session.GetPlayerName(PieceColor.Black));
        Assert.Equal(1, session.SeatOf(PieceColor.White));
        Assert.Equal(0, session.SeatOf(PieceColor.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SetSeat(2, "X", null, PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0052:UT-02 — lance legal na vez entra no histórico e passa o relógio")]
    [Trait("Category", "SPEC-0052:UT-02")]
    public void LegalMove_ShouldBeRecordedAndPressClock()
    {
        var time = new ManualTime();
        using var session = New(time);
        time.Advance(20 * Second);

        var ok = session.TryMove(PieceColor.White, Square.Parse("e2"), Square.Parse("e4"), null, out var played);
        time.Advance(10 * Second);
        var snap = session.Snapshot();

        Assert.True(ok);
        Assert.Equal("e4", played!.San);
        Assert.Equal(["e4"], snap.Moves.Select(m => m.San));
        Assert.Equal(PieceColor.Black, snap.SideToMove);
        Assert.Equal(PieceColor.Black, snap.ClockRunning);
        Assert.Equal(TimeControl.Blitz.Initial, snap.WhiteRemaining);
        Assert.Equal(TimeControl.Blitz.Initial - 10 * Second, snap.BlackRemaining);
    }

    [Fact(DisplayName = "SPEC-0052:UT-02 — fora da vez, ilegal ou promoção ausente retornam falso sem mudar o estado")]
    [Trait("Category", "SPEC-0052:UT-02")]
    public void InvalidMoves_ShouldBeRejected()
    {
        using var session = New(start: Position.FromFen("8/P6k/8/8/8/8/7p/K7 w - - 0 1"));
        var before = session.Snapshot();

        Assert.False(Play(session, PieceColor.Black, "h7", "h6"));
        Assert.False(Play(session, PieceColor.White, "a1", "a8"));
        Assert.False(Play(session, PieceColor.White, "a7", "a8"));
        Assert.False(Play(session, PieceColor.White, "a7", "a8", PieceType.King));
        AssertSameState(before, session.Snapshot());
        Assert.True(Play(session, PieceColor.White, "a7", "a8", PieceType.Queen));
        Assert.Equal("a8=Q", session.Snapshot().Moves[0].San.TrimEnd('+', '#'));
    }

    [Fact(DisplayName = "SPEC-0052:UT-02 — lance depois do fim é recusado")]
    [Trait("Category", "SPEC-0052:UT-02")]
    public void MoveAfterEnd_ShouldBeRejected()
    {
        using var session = New();
        Line(session, "f2f3", "e7e5", "g2g4", "d8h4");
        var before = session.Snapshot();

        Assert.False(Play(session, PieceColor.White, "a2", "a3"));
        AssertSameState(before, session.Snapshot());
    }

    [Fact(DisplayName = "SPEC-0052:UT-03 — mate do pastor encerra a sessão por regras")]
    [Trait("Category", "SPEC-0052:UT-03")]
    public void ScholarsMate_ShouldEndSession()
    {
        var time = new ManualTime();
        using var session = New(time);
        Line(session, "e2e4", "e7e5", "f1c4", "b8c6", "d1h5", "g8f6");
        Assert.False(session.IsOver);
        Assert.Null(session.EndedAtUtc);
        Assert.Null(session.Duration);
        time.Advance(7 * Second);

        Line(session, "h5f7");
        time.Advance(30 * Second);
        var snap = session.Snapshot();

        Assert.True(session.IsOver);
        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Checkmate), session.Result);
        Assert.Equal(session.Result, snap.Result);
        Assert.Null(snap.ClockRunning);
        Assert.Equal(snap.BlackRemaining, session.Snapshot().BlackRemaining);
        Assert.Equal(session.StartedAtUtc + 7 * Second, session.EndedAtUtc);
        Assert.Equal(7 * Second, session.Duration);
    }

    [Fact(DisplayName = "SPEC-0052:UT-03 — afogamento é empate e para o relógio")]
    [Trait("Category", "SPEC-0052:UT-03")]
    public void Stalemate_ShouldEndAsDraw()
    {
        using var session = New(start: Position.FromFen("7k/8/5K2/8/8/8/8/6Q1 w - - 0 1"));

        Line(session, "g1g6");

        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.Stalemate), session.Result);
        Assert.True(session.IsOver);
        Assert.Null(session.Snapshot().ClockRunning);
        Assert.NotNull(session.EndedAtUtc);
        Assert.NotNull(session.Duration);
    }

    [Fact(DisplayName = "SPEC-0052:UT-04 — Tick derruba a bandeira e o outro vence por tempo")]
    [Trait("Category", "SPEC-0052:UT-04")]
    public void Tick_ShouldEndByTimeout()
    {
        var time = new ManualTime();
        using var session = New(time);
        Line(session, "e2e4");

        time.Advance(TimeControl.Blitz.Initial + Second);
        session.Tick();

        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Timeout), session.Result);
        Assert.True(session.IsOver);
        Assert.NotNull(session.EndedAtUtc);
        var snap = session.Snapshot();
        Assert.Equal(TimeSpan.Zero, snap.BlackRemaining);
        Assert.Null(snap.ClockRunning);
    }

    [Fact(DisplayName = "SPEC-0052:UT-04 — lance depois da bandeira sem Tick é recusado e encerra por tempo")]
    [Trait("Category", "SPEC-0052:UT-04")]
    public void MoveAfterFlag_ShouldBeRejectedAndEndByTimeout()
    {
        var time = new ManualTime();
        using var session = New(time);
        Line(session, "e2e4");
        time.Advance(TimeControl.Blitz.Initial + Second);

        var ok = Play(session, PieceColor.Black, "e7", "e5");

        Assert.False(ok);
        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Timeout), session.Result);
        Assert.Single(session.Snapshot().Moves);
    }

    [Fact(DisplayName = "SPEC-0052:UT-04 — vencedor por tempo sem material de mate vira empate")]
    [Trait("Category", "SPEC-0052:UT-04")]
    public void Timeout_WithoutMatingMaterial_ShouldBeDraw()
    {
        var time = new ManualTime();
        using var session = New(time, start: Position.FromFen("4k3/4p3/8/8/8/8/8/4K3 w - - 0 1"));
        Line(session, "e1e2");

        time.Advance(TimeControl.Blitz.Initial + Second);
        session.Tick();

        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.Timeout), session.Result);
    }

    [Fact(DisplayName = "SPEC-0052:UT-04 — vencedor por tempo só com rei e peça menor empata; com torre vence")]
    [Trait("Category", "SPEC-0052:UT-04")]
    public void Timeout_MatingMaterialDecidesDrawOrWin()
    {
        var time = new ManualTime();
        using var minor = New(time, start: Position.FromFen("4k3/4p3/8/8/8/8/8/2B1K3 w - - 0 1"));
        Line(minor, "c1d2");
        using var rook = New(time, start: Position.FromFen("4k3/4p3/8/8/8/8/8/R3K3 w - - 0 1"));
        Line(rook, "a1a2");

        time.Advance(TimeControl.Blitz.Initial + Second);
        minor.Tick();
        rook.Tick();

        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Timeout), rook.Result);
        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.Timeout), minor.Result);
    }

    [Fact(DisplayName = "SPEC-0052:UT-06 — OnStateChanged roda fora do lock (lance e bandeira)")]
    [Trait("Category", "SPEC-0052:UT-06")]
    public void Event_ShouldRunOutsideLock()
    {
        var time = new ManualTime();
        using var session = New(time);
        var probes = 0;
        var blocked = 0;
        session.OnStateChanged += () =>
        {
            probes++;
            var probe = Task.Run(() => session.Snapshot());
            if (!probe.Wait(TimeSpan.FromSeconds(5)))
            {
                blocked++;
            }
        };

        Line(session, "e2e4");
        time.Advance(TimeControl.Blitz.Initial + Second);
        session.Tick();

        Assert.Equal(0, blocked);
        Assert.True(probes >= 2);
        Assert.True(session.IsOver);
    }

    [Fact(DisplayName = "SPEC-0052:UT-06 — um evento por comando efetivo e nenhum por recusado")]
    [Trait("Category", "SPEC-0052:UT-06")]
    public void Event_ShouldFireOncePerEffectiveCommand()
    {
        using var session = New();
        var count = 0;
        session.OnStateChanged += () => count++;

        Assert.False(Play(session, PieceColor.Black, "e7", "e5"));
        Assert.Equal(0, count);
        Line(session, "e2e4");
        Assert.Equal(1, count);
        session.Tick();
        Assert.Equal(1, count);
    }

    [Fact(DisplayName = "SPEC-0052:UT-07 — TryMarkResultRecorded é verdadeiro só uma vez")]
    [Trait("Category", "SPEC-0052:UT-07")]
    public void ResultRecorded_ShouldBeTrueOnlyOnce()
    {
        using var session = New();
        Assert.False(session.TryMarkResultRecorded());
        Line(session, "f2f3", "e7e5", "g2g4", "d8h4");

        Assert.True(session.TryMarkResultRecorded());
        Assert.False(session.TryMarkResultRecorded());
    }

    [Fact(DisplayName = "SPEC-0052:UT-07 — chamadas concorrentes têm um único vencedor")]
    [Trait("Category", "SPEC-0052:UT-07")]
    public async Task ResultRecorded_ConcurrentCallers_SingleWinner()
    {
        using var session = New();
        Line(session, "f2f3", "e7e5", "g2g4", "d8h4");
        using var gate = new Barrier(8);

        var wins = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                gate.SignalAndWait();
                return session.TryMarkResultRecorded();
            }))
            .ToArray();
        var results = await Task.WhenAll(wins);

        Assert.Equal(1, results.Count(r => r));
    }

    [Fact(DisplayName = "SPEC-0052:UT-04 — RestartCore reinicia partida, relógio e cores")]
    [Trait("Category", "SPEC-0052:UT-04")]
    public void RestartCore_ShouldResetGameClockAndSwapColors()
    {
        var time = new ManualTime();
        using var session = New(time);
        Line(session, "f2f3", "e7e5", "g2g4", "d8h4");
        Assert.True(session.TryMarkResultRecorded());
        time.Advance(5 * Second);

        var restarted = session.RestartCore(swapColors: true);
        var snap = session.Snapshot();

        Assert.True(restarted);
        Assert.False(session.IsOver);
        Assert.Empty(snap.Moves);
        Assert.Equal(TimeControl.Blitz.Initial, snap.WhiteRemaining);
        Assert.Equal(PieceColor.Black, session.ColorOf(0));
        Assert.Equal(PieceColor.White, session.ColorOf(1));
        Assert.Equal("Bia", snap.WhiteName);
        Assert.Equal(session.StartedAtUtc, time.GetUtcNow());
        Assert.Null(session.EndedAtUtc);
        Assert.False(session.TryMarkResultRecorded());
    }

    [Fact(DisplayName = "SPEC-0052:IT-01 — mate do pastor com relógio e depois partida por tempo")]
    [Trait("Category", "SPEC-0052:IT-01")]
    public void FullFlows_ShouldWorkEndToEnd()
    {
        var time = new ManualTime();
        using var session = New(time, TimeControl.Rapid);
        session.Mode = ChessMode.Private;
        var moves = new[] { "e2e4", "e7e5", "f1c4", "b8c6", "d1h5", "g8f6", "h5f7" };
        foreach (var move in moves)
        {
            time.Advance(3 * Second);
            Line(session, move);
        }

        var snap = session.Snapshot();
        Assert.Equal(ChessMode.Private, snap.Mode);
        Assert.Equal(7, snap.Moves.Count);
        Assert.Equal("Qxf7#", snap.Moves[^1].San);
        Assert.Equal([PieceType.Pawn], snap.CapturedByWhite);
        Assert.Empty(snap.CapturedByBlack);
        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Checkmate), snap.Result);
        Assert.Equal("Ana", snap.WhiteName);
        Assert.Equal("Bia", snap.BlackName);
        Assert.Equal(session.Id, snap.SessionId);
        Assert.Equal(TimeControl.Rapid, snap.Control);
        Assert.Equal(TimeControl.Rapid.Initial - (3 * 3 * Second) + (4 * TimeControl.Rapid.Increment), snap.WhiteRemaining);
        Assert.Equal(TimeControl.Rapid.Initial - (3 * 3 * Second) + (3 * TimeControl.Rapid.Increment), snap.BlackRemaining);

        using var timed = New(time, TimeControl.Bullet);
        Line(timed, "d2d4");
        time.Advance(TimeControl.Bullet.Initial - Second);
        Assert.False(timed.IsOver);
        time.Advance(2 * Second);
        var flagged = timed.Snapshot();

        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Timeout), flagged.Result);
        Assert.Equal(TimeSpan.Zero, flagged.BlackRemaining);
        Assert.NotNull(flagged.EndedAtUtc);
    }
}
