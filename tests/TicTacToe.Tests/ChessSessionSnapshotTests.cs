using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessSessionSnapshotTests
{
    [Fact(DisplayName = "SPEC-0052:UT-05 — snapshots antigos são imutáveis")]
    [Trait("Category", "SPEC-0052:UT-05")]
    public void Snapshot_ShouldBeImmutable()
    {
        using var session = ChessSessionTests.New();
        var empty = session.Snapshot();
        ChessSessionTests.Line(session, "e2e4", "d7d5");
        var middle = session.Snapshot();
        ChessSessionTests.Line(session, "e4d5");
        var last = session.Snapshot();

        Assert.Empty(empty.Moves);
        Assert.Equal(2, middle.Moves.Count);
        Assert.Empty(middle.CapturedByWhite);
        Assert.Equal(3, last.Moves.Count);
        Assert.Equal([PieceType.Pawn], last.CapturedByWhite);
        Assert.Equal(PieceColor.White, empty.SideToMove);
        Assert.Equal(PieceColor.White, middle.SideToMove);
        Assert.Equal(PieceColor.Black, last.SideToMove);
        Assert.NotSame(session.Snapshot().Moves, session.Snapshot().Moves);
    }

    [Fact(DisplayName = "SPEC-0052:UT-05 — leitura concorrente com 40 lances é sempre consistente (50 repetições)")]
    [Trait("Category", "SPEC-0052:UT-05")]
    public async Task Snapshot_ConcurrentReadsDuringMoves_ShouldBeConsistent()
    {
        for (var round = 0; round < 50; round++)
        {
            using var session = ChessSessionTests.New();
            var done = false;
            var inconsistent = 0;
            var reads = 0;
            Exception? failure = null;
            using var started = new ManualResetEventSlim();

            var reader = Task.Run(() =>
            {
                try
                {
                    started.Set();
                    while (!Volatile.Read(ref done))
                    {
                        var snap = session.Snapshot();
                        var expected = ((snap.Position.FullmoveNumber - 1) * 2) + (snap.Position.SideToMove == PieceColor.Black ? 1 : 0);
                        if (snap.Moves.Count != expected || snap.SideToMove != snap.Position.SideToMove)
                        {
                            Interlocked.Increment(ref inconsistent);
                        }

                        _ = snap.Moves.Count(m => m.IsCheck);
                        Interlocked.Increment(ref reads);
                    }
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            started.Wait();
            var random = new Random(round);
            for (var i = 0; i < 40 && !session.IsOver; i++)
            {
                var snap = session.Snapshot();
                var legal = snap.Position.LegalMoves();
                var move = legal[random.Next(legal.Count)];
                var promotion = move.Promotion ?? (PieceType?)null;
                Assert.True(session.TryMove(snap.SideToMove, move.From, move.To, promotion, out _));
            }

            Volatile.Write(ref done, true);
            await reader;

            Assert.Null(failure);
            Assert.Equal(0, inconsistent);
            Assert.True(reads > 0);
        }
    }
}
