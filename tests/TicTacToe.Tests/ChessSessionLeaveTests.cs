using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessSessionLeaveTests
{
    internal const string MateInOneFen = "6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1";

    internal static void FoolsMate(ChessSession session) =>
        ChessSessionTests.Line(session, "f2f3", "e7e5", "g2g4", "d8h4");

    [Fact(DisplayName = "SPEC-0061:UT-01 — Forfeit: o outro lado vence com o motivo dado e o perdedor fica ausente")]
    [Trait("Category", "SPEC-0061:UT-01")]
    public void Forfeit_ShouldEndWithGivenReasonAndMarkLoserAsLeft()
    {
        using var session = ChessSessionTests.New();

        var forfeited = session.Forfeit(PieceColor.White, ChessEndReason.Resignation);

        Assert.True(forfeited);
        Assert.Equal(new ChessResult(ChessOutcome.BlackWins, ChessEndReason.Resignation), session.Result);
        Assert.NotNull(session.EndedAtUtc);
        Assert.True(session.HasLeft(PieceColor.White));
        Assert.False(session.HasLeft(PieceColor.Black));
    }

    [Theory(DisplayName = "SPEC-0061:UT-01 — Forfeit aceita Resignation, Abandon, Disconnect e Timeout")]
    [Trait("Category", "SPEC-0061:UT-01")]
    [InlineData(ChessEndReason.Resignation)]
    [InlineData(ChessEndReason.Abandon)]
    [InlineData(ChessEndReason.Disconnect)]
    [InlineData(ChessEndReason.Timeout)]
    public void Forfeit_ShouldAcceptForfeitReasons(ChessEndReason reason)
    {
        using var session = ChessSessionTests.New();

        Assert.True(session.Forfeit(PieceColor.Black, reason));

        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, reason), session.Result);
    }

    [Theory(DisplayName = "SPEC-0061:UT-01 — Forfeit rejeita motivo que não é de desistência")]
    [Trait("Category", "SPEC-0061:UT-01")]
    [InlineData(ChessEndReason.Checkmate)]
    [InlineData(ChessEndReason.Stalemate)]
    public void Forfeit_ShouldRejectNonForfeitReason(ChessEndReason reason)
    {
        using var session = ChessSessionTests.New();

        Assert.False(session.Forfeit(PieceColor.White, reason));

        Assert.Null(session.Result);
        Assert.False(session.HasLeft(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-01 — Forfeit em partida encerrada retorna falso e não altera o resultado")]
    [Trait("Category", "SPEC-0061:UT-01")]
    public void Forfeit_WhenOver_ShouldReturnFalse()
    {
        using var session = ChessSessionTests.New();
        FoolsMate(session);
        var before = session.Result;

        Assert.False(session.Forfeit(PieceColor.Black, ChessEndReason.Resignation));

        Assert.Equal(before, session.Result);
        Assert.False(session.HasLeft(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0061:UT-01 — Forfeit repetido é idempotente")]
    [Trait("Category", "SPEC-0061:UT-01")]
    public void Forfeit_Twice_ShouldBeIdempotent()
    {
        using var session = ChessSessionTests.New();

        Assert.True(session.Forfeit(PieceColor.White, ChessEndReason.Resignation));
        Assert.False(session.Forfeit(PieceColor.Black, ChessEndReason.Resignation));

        Assert.Equal(ChessOutcome.BlackWins, session.Result!.Outcome);
    }

    [Fact(DisplayName = "SPEC-0061:UT-01 — Leave em partida em andamento derrota por Abandon")]
    [Trait("Category", "SPEC-0061:UT-01")]
    public void Leave_InProgress_ShouldForfeitByAbandon()
    {
        using var session = ChessSessionTests.New();
        ChessSessionTests.Line(session, "e2e4");

        var result = session.Leave(PieceColor.Black);

        Assert.Equal(ChessLeaveResult.Forfeited, result);
        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Abandon), session.Result);
        Assert.True(session.HasLeft(PieceColor.Black));
        Assert.True(session.TryMarkResultRecorded());
    }

    [Fact(DisplayName = "SPEC-0061:UT-01 — Leave com a partida encerrada devolve Left")]
    [Trait("Category", "SPEC-0061:UT-01")]
    public void Leave_WhenOver_ShouldReturnLeft()
    {
        using var session = ChessSessionTests.New();
        FoolsMate(session);
        var before = session.Result;

        var result = session.Leave(PieceColor.White);

        Assert.Equal(ChessLeaveResult.Left, result);
        Assert.Equal(before, session.Result);
        Assert.True(session.HasLeft(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-01 — Leave repetido é Rejected")]
    [Trait("Category", "SPEC-0061:UT-01")]
    public void Leave_Twice_ShouldBeRejected()
    {
        using var session = ChessSessionTests.New();

        Assert.Equal(ChessLeaveResult.Forfeited, session.Leave(PieceColor.White));
        Assert.Equal(ChessLeaveResult.Rejected, session.Leave(PieceColor.White));
        Assert.Equal(ChessLeaveResult.Left, session.Leave(PieceColor.Black));
        Assert.Equal(ChessLeaveResult.Rejected, session.Leave(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0061:UT-06 — Leave em solo em andamento descarta a partida sem resultado gravável")]
    [Trait("Category", "SPEC-0061:UT-06")]
    public void Leave_Solo_ShouldDiscardWithoutRecordableResult()
    {
        using var session = ChessSessionTests.New();
        session.Mode = ChessMode.Solo;
        ChessSessionTests.Line(session, "e2e4");

        var result = session.Leave(PieceColor.White);

        Assert.Equal(ChessLeaveResult.Discarded, result);
        Assert.Null(session.Result);
        Assert.True(session.HasLeft(PieceColor.White));
        Assert.False(session.TryMarkResultRecorded());
    }

    [Fact(DisplayName = "SPEC-0061:UT-06 — Abandono e revanche em solo não gravam resultado")]
    [Trait("Category", "SPEC-0061:UT-06")]
    public void Solo_LeaveThenRematch_ShouldBeRejected()
    {
        using var session = ChessSessionTests.New();
        session.Mode = ChessMode.Solo;
        session.Leave(PieceColor.White);

        Assert.False(session.RequestRematch(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-05 — Lance e Leave concorrentes produzem um único resultado (50 repetições)")]
    [Trait("Category", "SPEC-0061:UT-05")]
    public async Task MoveAndLeave_Race_ShouldProduceSingleResult()
    {
        for (var i = 0; i < 50; i++)
        {
            using var session = ChessSessionTests.New(start: Position.FromFen(MateInOneFen));
            using var barrier = new Barrier(2);
            var moved = false;
            var left = ChessLeaveResult.Rejected;

            var mover = Task.Run(() =>
            {
                barrier.SignalAndWait();
                moved = ChessSessionTests.Play(session, PieceColor.White, "a1", "a8");
            });
            var leaver = Task.Run(() =>
            {
                barrier.SignalAndWait();
                left = session.Leave(PieceColor.Black);
            });
            await Task.WhenAll(mover, leaver);

            var result = session.Result;
            Assert.NotNull(result);
            if (moved)
            {
                Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Checkmate), result);
                Assert.Equal(ChessLeaveResult.Left, left);
            }
            else
            {
                Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Abandon), result);
                Assert.Equal(ChessLeaveResult.Forfeited, left);
            }

            Assert.True(session.TryMarkResultRecorded());
            Assert.False(session.TryMarkResultRecorded());
        }
    }

    [Fact(DisplayName = "SPEC-0061:UT-08 — OnStateChanged de Leave é disparado fora do lock")]
    [Trait("Category", "SPEC-0061:UT-08")]
    public void Leave_ShouldRaiseEventOutsideLock()
    {
        using var session = ChessSessionTests.New();
        var completed = false;
        session.OnStateChanged += () => completed = Task.Run(() => session.Snapshot()).Wait(TimeSpan.FromSeconds(5));

        session.Leave(PieceColor.White);

        Assert.True(completed);
    }
}
