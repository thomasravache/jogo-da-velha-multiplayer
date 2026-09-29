using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessSessionRematchTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    private static ChessSession Finished(ManualTime? time = null)
    {
        var session = ChessSessionTests.New(time);
        ChessSessionLeaveTests.FoolsMate(session);
        return session;
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Pedido de revanche fica Requested com a cor de quem pediu")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void RequestRematch_ShouldBeRequested()
    {
        using var session = Finished();

        Assert.True(session.RequestRematch(PieceColor.White));

        Assert.Equal(ChessRematchState.Requested, session.RematchState);
        Assert.Equal(PieceColor.White, session.RematchRequestedBy);
        Assert.True(session.IsOver);
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Revanche só com a partida encerrada e sem pedido repetido")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void RequestRematch_ShouldValidateState()
    {
        using var running = ChessSessionTests.New();
        Assert.False(running.RequestRematch(PieceColor.White));
        Assert.Equal(ChessRematchState.None, running.RematchState);

        using var session = Finished();
        Assert.True(session.RequestRematch(PieceColor.White));
        Assert.False(session.RequestRematch(PieceColor.White));
        Assert.False(session.AcceptRematch(PieceColor.White));
        Assert.Equal(ChessRematchState.Requested, session.RematchState);
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Aceite reinicia com cores dos assentos trocadas e relógio novo")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void AcceptRematch_ShouldRestartWithSwappedColors()
    {
        var time = new ManualTime();
        using var session = Finished(time);
        time.Advance(10 * Second);
        session.RequestRematch(PieceColor.White);

        Assert.True(session.AcceptRematch(PieceColor.Black));

        Assert.Equal(ChessRematchState.None, session.RematchState);
        Assert.Null(session.RematchRequestedBy);
        Assert.Equal(PieceColor.Black, session.ColorOf(0));
        Assert.Equal(PieceColor.White, session.ColorOf(1));
        var snapshot = session.Snapshot();
        Assert.Null(snapshot.Result);
        Assert.Empty(snapshot.Moves);
        Assert.Equal(Position.Start.ToFen(), snapshot.Position.ToFen());
        Assert.Equal(TimeControl.Blitz.Initial, snapshot.WhiteRemaining);
        Assert.Equal(TimeControl.Blitz.Initial, snapshot.BlackRemaining);
        Assert.Equal("Bia", snapshot.WhiteName);
        Assert.Equal("Ana", snapshot.BlackName);
        Assert.Equal(time.GetUtcNow(), snapshot.StartedAtUtc);
        Assert.Null(snapshot.EndedAtUtc);
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Revanche reinicia da posição inicial configurada")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void AcceptRematch_ShouldRestartFromConfiguredStart()
    {
        var start = Position.FromFen(ChessSessionLeaveTests.MateInOneFen);
        using var session = ChessSessionTests.New(start: start);
        ChessSessionTests.Play(session, PieceColor.White, "a1", "a8");
        session.RequestRematch(PieceColor.White);

        Assert.True(session.AcceptRematch(PieceColor.Black));

        Assert.Equal(start.ToFen(), session.Snapshot().Position.ToFen());
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Recusa marca Declined e permite novo pedido")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void DeclineRematch_ShouldMarkDeclined()
    {
        using var session = Finished();
        session.RequestRematch(PieceColor.White);

        Assert.False(session.DeclineRematch(PieceColor.White));
        Assert.True(session.DeclineRematch(PieceColor.Black));

        Assert.Equal(ChessRematchState.Declined, session.RematchState);
        Assert.False(session.AcceptRematch(PieceColor.Black));
        Assert.True(session.IsOver);
        Assert.True(session.RequestRematch(PieceColor.Black));
        Assert.Equal(ChessRematchState.Requested, session.RematchState);
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Pedidos dos dois lados aceitam automaticamente")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void SimultaneousRequests_ShouldAutoAccept()
    {
        using var session = Finished();

        Assert.True(session.RequestRematch(PieceColor.White));
        Assert.True(session.RequestRematch(PieceColor.Black));

        Assert.False(session.IsOver);
        Assert.Equal(ChessRematchState.None, session.RematchState);
        Assert.Equal(PieceColor.Black, session.ColorOf(0));
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Pedido expira em 30 s e permite novo pedido")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void Request_ShouldExpireAfterThirtySeconds()
    {
        var time = new ManualTime();
        using var session = Finished(time);
        session.RequestRematch(PieceColor.White);

        time.Advance(29 * Second);
        session.Tick();
        Assert.Equal(ChessRematchState.Requested, session.RematchState);

        time.Advance(Second);
        session.Tick();
        Assert.Equal(ChessRematchState.Expired, session.RematchState);
        Assert.False(session.AcceptRematch(PieceColor.Black));
        Assert.True(session.RequestRematch(PieceColor.Black));
        Assert.Equal(ChessRematchState.Requested, session.RematchState);
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Pedido com o oponente ausente é recusado")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void Request_WhenOpponentLeft_ShouldBeRefused()
    {
        using var session = Finished();
        session.Leave(PieceColor.Black);

        Assert.False(session.RequestRematch(PieceColor.White));
        Assert.Equal(ChessRematchState.None, session.RematchState);
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Quem saiu não pede nem aceita revanche")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void Request_WhenSelfLeft_ShouldBeRefused()
    {
        using var session = Finished();
        session.RequestRematch(PieceColor.White);
        session.Leave(PieceColor.Black);

        Assert.False(session.AcceptRematch(PieceColor.Black));
        Assert.False(session.RequestRematch(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0061:UT-03 — Saída do solicitante depois do pedido invalida o aceite")]
    [Trait("Category", "SPEC-0061:UT-03")]
    public void Accept_WhenRequesterLeft_ShouldBeRefused()
    {
        using var session = Finished();
        session.RequestRematch(PieceColor.White);
        session.Leave(PieceColor.White);

        Assert.False(session.AcceptRematch(PieceColor.Black));
        Assert.True(session.IsOver);
    }

    [Fact(DisplayName = "SPEC-0061:UT-04 — Presença e saída seguem o assento após a troca de cores")]
    [Trait("Category", "SPEC-0061:UT-04")]
    public void SwappedColors_ShouldKeepLeaveAndHasLeftPerSeat()
    {
        using var session = Finished();
        session.RequestRematch(PieceColor.White);
        session.AcceptRematch(PieceColor.Black);
        Assert.Equal(0, session.SeatOf(PieceColor.Black));

        var result = session.Leave(PieceColor.Black);

        Assert.Equal(ChessLeaveResult.Forfeited, result);
        Assert.True(session.HasLeft(PieceColor.Black));
        Assert.False(session.HasLeft(PieceColor.White));
        Assert.Equal(ChessOutcome.WhiteWins, session.Result!.Outcome);
        Assert.Equal(1, session.SeatOf(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-04 — Saída anterior à revanche é limpa no reinício")]
    [Trait("Category", "SPEC-0061:UT-04")]
    public void Restart_ShouldClearPresenceAndLeft()
    {
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        session.SetConnection(PieceColor.Black, false);
        ChessSessionLeaveTests.FoolsMate(session);
        session.RequestRematch(PieceColor.White);
        session.AcceptRematch(PieceColor.Black);

        Assert.False(session.HasLeft(PieceColor.White));
        Assert.False(session.HasLeft(PieceColor.Black));
        Assert.Null(session.DisconnectSecondsLeft(PieceColor.Black));
        Assert.Null(session.DisconnectSecondsLeft(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0061:UT-05 — Dois aceites em paralelo reiniciam uma única vez (50 repetições)")]
    [Trait("Category", "SPEC-0061:UT-05")]
    public async Task ConcurrentAccepts_ShouldRestartOnce()
    {
        for (var i = 0; i < 50; i++)
        {
            using var session = Finished();
            session.RequestRematch(PieceColor.White);
            using var barrier = new Barrier(2);
            var accepted = new bool[2];

            var tasks = new[]
            {
                Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    accepted[0] = session.AcceptRematch(PieceColor.Black);
                }),
                Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    accepted[1] = session.AcceptRematch(PieceColor.Black);
                }),
            };
            await Task.WhenAll(tasks);

            Assert.Equal(1, accepted.Count(a => a));
            Assert.Equal(PieceColor.Black, session.ColorOf(0));
            Assert.False(session.IsOver);
            Assert.Equal(ChessRematchState.None, session.RematchState);
        }
    }

    [Fact(DisplayName = "SPEC-0061:UT-05 — Pedidos simultâneos dos dois lados reiniciam uma única vez (50 repetições)")]
    [Trait("Category", "SPEC-0061:UT-05")]
    public async Task ConcurrentRequests_ShouldRestartOnce()
    {
        for (var i = 0; i < 50; i++)
        {
            using var session = Finished();
            using var barrier = new Barrier(2);

            var white = Task.Run(() =>
            {
                barrier.SignalAndWait();
                session.RequestRematch(PieceColor.White);
            });
            var black = Task.Run(() =>
            {
                barrier.SignalAndWait();
                session.RequestRematch(PieceColor.Black);
            });
            await Task.WhenAll(white, black);

            Assert.False(session.IsOver);
            Assert.Equal(PieceColor.Black, session.ColorOf(0));
        }
    }

    [Fact(DisplayName = "SPEC-0061:UT-06 — Revanche em solo reinicia na hora com cores trocadas")]
    [Trait("Category", "SPEC-0061:UT-06")]
    public void Solo_RequestRematch_ShouldRestartImmediately()
    {
        using var session = Finished();
        session.Mode = ChessMode.Solo;

        Assert.True(session.RequestRematch(PieceColor.White));

        Assert.False(session.IsOver);
        Assert.Equal(ChessRematchState.None, session.RematchState);
        Assert.Equal(PieceColor.Black, session.ColorOf(0));
        Assert.Equal(PieceColor.White, session.ColorOf(1));
    }

    [Fact(DisplayName = "SPEC-0061:UT-07 — TryMarkResultRecorded volta a valer após a revanche")]
    [Trait("Category", "SPEC-0061:UT-07")]
    public void TryMarkResultRecorded_ShouldResetAfterRematch()
    {
        using var session = Finished();
        Assert.True(session.TryMarkResultRecorded());
        Assert.False(session.TryMarkResultRecorded());

        session.RequestRematch(PieceColor.White);
        session.AcceptRematch(PieceColor.Black);
        Assert.False(session.TryMarkResultRecorded());

        session.Forfeit(PieceColor.White, ChessEndReason.Resignation);

        Assert.True(session.TryMarkResultRecorded());
        Assert.False(session.TryMarkResultRecorded());
    }

    [Fact(DisplayName = "SPEC-0061:UT-08 — Consulta em handler não bloqueia em AcceptRematch")]
    [Trait("Category", "SPEC-0061:UT-08")]
    public void AcceptRematch_ShouldRaiseEventOutsideLock()
    {
        using var session = Finished();
        session.RequestRematch(PieceColor.White);
        var completed = false;
        session.OnStateChanged += () => completed = Task.Run(() => session.Snapshot()).Wait(TimeSpan.FromSeconds(5));

        session.AcceptRematch(PieceColor.Black);

        Assert.True(completed);
    }

    [Fact(DisplayName = "SPEC-0061:UT-08 — Consulta em handler não bloqueia quando o Tick expira um pedido")]
    [Trait("Category", "SPEC-0061:UT-08")]
    public void TickExpiry_ShouldRaiseEventOutsideLock()
    {
        var time = new ManualTime();
        using var session = Finished(time);
        session.RequestRematch(PieceColor.White);
        time.Advance(30 * Second);
        var completed = false;
        var raised = false;
        session.OnStateChanged += () =>
        {
            raised = true;
            completed = Task.Run(() => session.Snapshot()).Wait(TimeSpan.FromSeconds(5));
        };

        session.Tick();

        Assert.True(raised);
        Assert.True(completed);
        Assert.Equal(ChessRematchState.Expired, session.RematchState);
    }
}
