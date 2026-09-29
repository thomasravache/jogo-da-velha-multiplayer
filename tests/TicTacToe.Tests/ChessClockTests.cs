using TicTacToe.Modules.Chess;
using Xunit;

namespace TicTacToe.Tests;

public class ChessClockTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [Fact(DisplayName = "SPEC-0051:UT-01 — controles padrão e FromId")]
    [Trait("Category", "SPEC-0051:UT-01")]
    public void DefaultControls_MatchContract()
    {
        Assert.Equal(3, TimeControl.All.Count);
        Assert.Equal(new TimeControl("bullet1+0", "Bullet 1+0", TimeSpan.FromMinutes(1), TimeSpan.Zero), TimeControl.Bullet);
        Assert.Equal(new TimeControl("blitz5+0", "Blitz 5+0", TimeSpan.FromMinutes(5), TimeSpan.Zero), TimeControl.Blitz);
        Assert.Equal(new TimeControl("rapida10+5", "Rápida 10+5", TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(5)), TimeControl.Rapid);
        Assert.Equal([TimeControl.Bullet, TimeControl.Blitz, TimeControl.Rapid], TimeControl.All);
        Assert.Same(TimeControl.Blitz, TimeControl.FromId("blitz5+0"));
        Assert.Same(TimeControl.Rapid, TimeControl.FromId("rapida10+5"));
        Assert.Same(TimeControl.Bullet, TimeControl.FromId("bullet1+0"));
        Assert.Null(TimeControl.FromId("x"));
    }

    [Fact(DisplayName = "SPEC-0051:UT-02 — nada corre antes do primeiro lance; depois só o de quem joga")]
    [Trait("Category", "SPEC-0051:UT-02")]
    public void Counting_StartsOnlyAfterFirstMove()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Blitz, time);

        time.Advance(30 * Second);
        Assert.Equal(TimeControl.Blitz.Initial, clock.Remaining(PieceColor.White));
        Assert.Equal(TimeControl.Blitz.Initial, clock.Remaining(PieceColor.Black));
        Assert.Null(clock.Running);

        clock.Press(PieceColor.White);
        time.Advance(30 * Second);

        Assert.Equal(PieceColor.Black, clock.Running);
        Assert.Equal(TimeControl.Blitz.Initial - (30 * Second), clock.Remaining(PieceColor.Black));
        Assert.Equal(TimeControl.Blitz.Initial, clock.Remaining(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0051:UT-03 — incremento somado ao fim do lance")]
    [Trait("Category", "SPEC-0051:UT-03")]
    public void Increment_AddedAtEndOfMove()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Rapid, time);

        clock.Press(PieceColor.White);
        Assert.Equal(TimeControl.Rapid.Initial + TimeControl.Rapid.Increment, clock.Remaining(PieceColor.White));

        time.Advance(20 * Second);
        clock.Press(PieceColor.Black);

        Assert.Equal(TimeSpan.FromMinutes(10) - (20 * Second) + (5 * Second), clock.Remaining(PieceColor.Black));
        Assert.Equal(PieceColor.White, clock.Running);

        time.Advance(10 * Second);
        Assert.Equal(TimeSpan.FromMinutes(10) + (5 * Second) - (10 * Second), clock.Remaining(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0051:UT-04 — queda de bandeira para o relógio, sem incremento")]
    [Trait("Category", "SPEC-0051:UT-04")]
    public void Flag_FallsWhenTimeRunsOut()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Bullet, time);
        clock.Press(PieceColor.White);

        time.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(PieceColor.Black, clock.Flagged);
        Assert.Equal(TimeSpan.Zero, clock.Remaining(PieceColor.Black));
        Assert.Null(clock.Running);

        clock.Press(PieceColor.Black);
        time.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(PieceColor.Black, clock.Flagged);
        Assert.Equal(TimeSpan.Zero, clock.Remaining(PieceColor.Black));
        Assert.Equal(TimeControl.Bullet.Initial, clock.Remaining(PieceColor.White));
        Assert.Null(clock.Running);
    }

    [Fact(DisplayName = "SPEC-0051:UT-04 — Press tardio de quem caiu não soma incremento (10+5)")]
    [Trait("Category", "SPEC-0051:UT-04")]
    public void Flag_LatePressDoesNotAddIncrement()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Rapid, time);
        clock.Press(PieceColor.White);

        time.Advance(TimeSpan.FromMinutes(10) + Second);
        clock.Press(PieceColor.Black);

        Assert.Equal(PieceColor.Black, clock.Flagged);
        Assert.Equal(TimeSpan.Zero, clock.Remaining(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0051:UT-05 — Stop congela e Press é ignorado")]
    [Trait("Category", "SPEC-0051:UT-05")]
    public void Stop_FreezesClock()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Rapid, time);
        clock.Press(PieceColor.White);
        time.Advance(10 * Second);

        clock.Stop();
        var black = clock.Remaining(PieceColor.Black);
        var white = clock.Remaining(PieceColor.White);
        time.Advance(TimeSpan.FromMinutes(20));
        clock.Press(PieceColor.Black);

        Assert.Equal(TimeControl.Rapid.Initial - (10 * Second), black);
        Assert.Equal(black, clock.Remaining(PieceColor.Black));
        Assert.Equal(white, clock.Remaining(PieceColor.White));
        Assert.Null(clock.Running);
        Assert.Null(clock.Flagged);
    }

    [Fact(DisplayName = "SPEC-0051:UT-06 — consultas repetidas são idempotentes")]
    [Trait("Category", "SPEC-0051:UT-06")]
    public void RepeatedQueries_HaveNoSideEffects()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Blitz, time);
        clock.Press(PieceColor.White);
        time.Advance(42 * Second);

        var black = clock.Remaining(PieceColor.Black);
        for (var i = 0; i < 5; i++)
        {
            clock.Tick();
            Assert.Equal(black, clock.Remaining(PieceColor.Black));
            Assert.Null(clock.Flagged);
            Assert.Equal(PieceColor.Black, clock.Running);
        }

        Assert.Equal(TimeControl.Blitz.Initial - (42 * Second), black);
    }

    [Fact(DisplayName = "SPEC-0051:IT-01 — fluxo de dez lances com incremento e bandeira")]
    [Trait("Category", "SPEC-0051:IT-01")]
    public void FullGameFlow_TimesAndFlagAreConsistent()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Rapid, time);
        int[] seconds = [0, 12, 30, 5, 47, 9, 61, 3, 25, 14];
        var white = TimeControl.Rapid.Initial;
        var black = TimeControl.Rapid.Initial;
        var inc = TimeControl.Rapid.Increment;

        for (var i = 0; i < seconds.Length; i++)
        {
            var mover = i % 2 == 0 ? PieceColor.White : PieceColor.Black;
            var spent = seconds[i] * Second;
            time.Advance(spent);
            clock.Press(mover);
            if (mover == PieceColor.White)
            {
                white += inc - (i == 0 ? TimeSpan.Zero : spent);
            }
            else
            {
                black += inc - spent;
            }

            Assert.Null(clock.Flagged);
            Assert.Equal(white, clock.Remaining(PieceColor.White));
            Assert.Equal(black, clock.Remaining(PieceColor.Black));
        }

        Assert.Equal(PieceColor.White, clock.Running);

        time.Advance(white - Second);
        clock.Tick();
        Assert.Null(clock.Flagged);
        Assert.Equal(Second, clock.Remaining(PieceColor.White));

        time.Advance(Second);
        Assert.Equal(PieceColor.White, clock.Flagged);
        Assert.Equal(TimeSpan.Zero, clock.Remaining(PieceColor.White));
        Assert.Equal(black, clock.Remaining(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0051:UT-02 — Press(Black) antes do início é ignorado")]
    [Trait("Category", "SPEC-0051:UT-02")]
    public void Press_BlackBeforeStart_IsIgnored()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Rapid, time);

        clock.Press(PieceColor.Black);
        time.Advance(10 * Second);

        Assert.Null(clock.Running);
        Assert.Equal(TimeControl.Rapid.Initial, clock.Remaining(PieceColor.Black));
        Assert.Equal(TimeControl.Rapid.Initial, clock.Remaining(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0051:UT-03 — Press de quem não corre é ignorado")]
    [Trait("Category", "SPEC-0051:UT-03")]
    public void Press_ByNonRunningColor_IsIgnored()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Rapid, time);
        clock.Press(PieceColor.White);
        time.Advance(10 * Second);

        clock.Press(PieceColor.White);

        Assert.Equal(PieceColor.Black, clock.Running);
        Assert.Equal(TimeControl.Rapid.Initial + TimeControl.Rapid.Increment, clock.Remaining(PieceColor.White));
        Assert.Equal(TimeControl.Rapid.Initial - (10 * Second), clock.Remaining(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0051:UT-05 — Stop antes do início impede o início")]
    [Trait("Category", "SPEC-0051:UT-05")]
    public void Stop_BeforeStart_PreventsStart()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Rapid, time);

        clock.Stop();
        clock.Press(PieceColor.White);
        time.Advance(TimeSpan.FromMinutes(30));

        Assert.Null(clock.Running);
        Assert.Null(clock.Flagged);
        Assert.Equal(TimeControl.Rapid.Initial, clock.Remaining(PieceColor.White));
        Assert.Equal(TimeControl.Rapid.Initial, clock.Remaining(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0051:UT-05 — Stop após bandeira mantém a bandeira")]
    [Trait("Category", "SPEC-0051:UT-05")]
    public void Stop_AfterFlag_KeepsFlag()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Bullet, time);
        clock.Press(PieceColor.White);
        time.Advance(TimeSpan.FromMinutes(2));

        clock.Stop();

        Assert.Equal(PieceColor.Black, clock.Flagged);
        Assert.Equal(TimeSpan.Zero, clock.Remaining(PieceColor.Black));
        Assert.Null(clock.Running);
    }

    [Fact(DisplayName = "SPEC-0051:UT-04 — limite exato marca bandeira")]
    [Trait("Category", "SPEC-0051:UT-04")]
    public void Flag_ExactBoundary()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Blitz, time);
        clock.Press(PieceColor.White);

        time.Advance(TimeControl.Blitz.Initial - TimeSpan.FromMilliseconds(1));
        Assert.Null(clock.Flagged);
        Assert.Equal(TimeSpan.FromMilliseconds(1), clock.Remaining(PieceColor.Black));

        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(PieceColor.Black, clock.Flagged);
    }

    [Fact(DisplayName = "SPEC-0051:UT-04 — só Tick marca a bandeira")]
    [Trait("Category", "SPEC-0051:UT-04")]
    public void Flag_MarkedByTickAlone()
    {
        var time = new ManualTime();
        var clock = new ChessClock(TimeControl.Bullet, time);
        clock.Press(PieceColor.White);
        time.Advance(TimeSpan.FromMinutes(3));

        clock.Tick();

        Assert.Equal(PieceColor.Black, clock.Flagged);
        Assert.Null(clock.Running);
    }

    [Fact(DisplayName = "SPEC-0051:UT-06 — uso concorrente não lança nem gera tempo negativo")]
    [Trait("Category", "SPEC-0051:UT-06")]
    public async Task ConcurrentUse_IsSafe()
    {
        var clock = new ChessClock(TimeControl.Bullet, TimeProvider.System);
        clock.Press(PieceColor.White);
        var negative = 0;

        var tasks = Enumerable.Range(0, 8).Select(n => Task.Run(() =>
        {
            for (var i = 0; i < 20000; i++)
            {
                var color = (i + n) % 2 == 0 ? PieceColor.White : PieceColor.Black;
                clock.Press(color);
                clock.Tick();
                if (clock.Remaining(PieceColor.White) < TimeSpan.Zero || clock.Remaining(PieceColor.Black) < TimeSpan.Zero)
                {
                    Interlocked.Increment(ref negative);
                }

                _ = clock.Flagged;
                _ = clock.Running;
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(0, negative);
    }
}
