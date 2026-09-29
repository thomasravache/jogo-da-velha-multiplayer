using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessSessionPresenceTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [Fact(DisplayName = "SPEC-0061:UT-02 — Queda curta com retorno não encerra a partida")]
    [Trait("Category", "SPEC-0061:UT-02")]
    public void ShortDrop_ShouldKeepGoing()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);

        session.SetConnection(PieceColor.White, false);
        time.Advance(5 * Second);
        session.Tick();
        session.SetConnection(PieceColor.White, true);
        time.Advance(20 * Second);
        session.Tick();

        Assert.Null(session.Result);
        Assert.Null(session.DisconnectSecondsLeft(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-02 — 15 s de queda encerram por Disconnect")]
    [Trait("Category", "SPEC-0061:UT-02")]
    public void Drop_ShouldForfeitAfterGrace()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        session.SetConnection(PieceColor.White, false);

        time.Advance(14 * Second);
        session.Tick();
        Assert.Null(session.Result);

        time.Advance(Second);
        session.Tick();

        Assert.Equal(new ChessResult(ChessOutcome.BlackWins, ChessEndReason.Disconnect), session.Result);
        Assert.True(session.HasLeft(PieceColor.White));
        Assert.Null(session.DisconnectSecondsLeft(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-02 — Com dois desconectados perde quem estoura primeiro")]
    [Trait("Category", "SPEC-0061:UT-02")]
    public void TwoDropped_FirstToExpireLoses()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        session.SetConnection(PieceColor.Black, false);
        time.Advance(3 * Second);
        session.SetConnection(PieceColor.White, false);

        time.Advance(12 * Second);
        session.Tick();

        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Disconnect), session.Result);
        Assert.True(session.HasLeft(PieceColor.Black));
        Assert.False(session.HasLeft(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-02 — DisconnectSecondsLeft conta de 15 a 0")]
    [Trait("Category", "SPEC-0061:UT-02")]
    public void SecondsLeft_ShouldCountDown()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        Assert.Null(session.DisconnectSecondsLeft(PieceColor.White));

        session.SetConnection(PieceColor.White, false);
        Assert.Equal(15, session.DisconnectSecondsLeft(PieceColor.White));
        Assert.Equal(ChessSession.DisconnectGraceSeconds, session.DisconnectSecondsLeft(PieceColor.White));

        time.Advance(5 * Second);
        Assert.Equal(10, session.DisconnectSecondsLeft(PieceColor.White));

        time.Advance(10 * Second);
        Assert.Equal(0, session.DisconnectSecondsLeft(PieceColor.White));
        Assert.Null(session.DisconnectSecondsLeft(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0061:UT-02 — Queda é ignorada em solo")]
    [Trait("Category", "SPEC-0061:UT-02")]
    public void Solo_ShouldIgnoreConnection()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        session.Mode = ChessMode.Solo;

        session.SetConnection(PieceColor.White, false);
        time.Advance(60 * Second);
        session.Tick();

        Assert.Null(session.DisconnectSecondsLeft(PieceColor.White));
        Assert.Null(session.Result);
    }

    [Fact(DisplayName = "SPEC-0061:UT-02 — Queda é ignorada com a partida encerrada e a contagem some")]
    [Trait("Category", "SPEC-0061:UT-02")]
    public void Over_ShouldIgnoreConnectionAndClearCountdown()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        session.SetConnection(PieceColor.White, false);
        session.Forfeit(PieceColor.Black, ChessEndReason.Resignation);
        Assert.Null(session.DisconnectSecondsLeft(PieceColor.White));

        session.SetConnection(PieceColor.Black, false);

        Assert.Null(session.DisconnectSecondsLeft(PieceColor.Black));
        time.Advance(30 * Second);
        session.Tick();
        Assert.Equal(ChessEndReason.Resignation, session.Result!.Reason);
    }

    [Fact(DisplayName = "SPEC-0061:UT-02 — Queda de jogador ausente é ignorada")]
    [Trait("Category", "SPEC-0061:UT-02")]
    public void Left_ShouldIgnoreConnection()
    {
        using var session = ChessSessionTests.New();
        session.Leave(PieceColor.White);

        session.SetConnection(PieceColor.White, false);

        Assert.Null(session.DisconnectSecondsLeft(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-04 — Queda pela cor nova age sobre o assento certo")]
    [Trait("Category", "SPEC-0061:UT-04")]
    public void SwappedColors_ShouldTrackPresencePerSeat()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        ChessSessionLeaveTests.FoolsMate(session);
        session.RequestRematch(PieceColor.White);
        session.AcceptRematch(PieceColor.Black);
        Assert.Equal(PieceColor.Black, session.ColorOf(0));

        session.SetConnection(PieceColor.Black, false);
        time.Advance(15 * Second);
        session.Tick();

        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Disconnect), session.Result);
        Assert.True(session.HasLeft(PieceColor.Black));
        Assert.False(session.HasLeft(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-08 — Consulta em handler não bloqueia quando o Tick derruba por Disconnect")]
    [Trait("Category", "SPEC-0061:UT-08")]
    public void TickDisconnect_ShouldRaiseEventOutsideLock()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        session.SetConnection(PieceColor.White, false);
        time.Advance(15 * Second);
        var completed = false;
        session.OnStateChanged += () => completed = Task.Run(() => session.Snapshot()).Wait(TimeSpan.FromSeconds(5));

        session.Tick();

        Assert.True(completed);
        Assert.Equal(ChessEndReason.Disconnect, session.Result!.Reason);
    }

    [Fact(DisplayName = "SPEC-0061:IT-01 — Fluxo: abandono, queda e revanche aceita de ponta a ponta")]
    [Trait("Category", "SPEC-0061:IT-01")]
    public void FullFlow_ShouldProduceExpectedResults()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);

        // Partida 1: Bia abandona.
        ChessSessionTests.Line(session, "e2e4");
        Assert.Equal(ChessLeaveResult.Forfeited, session.Leave(PieceColor.Black));
        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Abandon), session.Result);
        Assert.False(session.RequestRematch(PieceColor.White));

        // Nova sessão: Ana (brancas) cai e perde por Disconnect; depois a revanche é recusada por ausência.
        using var second = ChessSessionTests.New(time);
        second.SetConnection(PieceColor.White, false);
        time.Advance(15 * Second);
        second.Tick();
        Assert.Equal(new ChessResult(ChessOutcome.BlackWins, ChessEndReason.Disconnect), second.Result);
        Assert.False(second.RequestRematch(PieceColor.Black));

        // Terceira: partida normal, revanche aceita troca cores e renova relógios.
        using var third = ChessSessionTests.New(time);
        ChessSessionTests.Line(third, "e2e4");
        time.Advance(20 * Second);
        third.Forfeit(PieceColor.Black, ChessEndReason.Resignation);
        third.RequestRematch(PieceColor.Black);
        Assert.True(third.AcceptRematch(PieceColor.White));

        var snapshot = third.Snapshot();
        Assert.Null(snapshot.Result);
        Assert.Equal("Bia", snapshot.WhiteName);
        Assert.Equal("Ana", snapshot.BlackName);
        Assert.Equal(TimeControl.Blitz.Initial, snapshot.WhiteRemaining);
        Assert.Equal(TimeControl.Blitz.Initial, snapshot.BlackRemaining);
    }
}
