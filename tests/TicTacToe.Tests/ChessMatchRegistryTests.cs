using TicTacToe.Modules.Chess;
using TicTacToe.Web.Services.Chess;

namespace TicTacToe.Tests;

// SPEC-0053: registro das partidas de xadrez (sessão única por matchId, assentos por conexão)

public class ChessMatchRegistryTests
{
    [Fact(DisplayName = "SPEC-0053:UT-05 — Factory roda uma só vez com dois circuitos em paralelo; cores opostas e estáveis")]
    [Trait("Category", "SPEC-0053:UT-05")]
    public async Task GetOrCreate_ShouldRunFactoryOnceAndKeepOppositeColors()
    {
        var registry = new ChessMatchRegistry();
        var matchId = Guid.NewGuid();
        var factoryRuns = 0;
        var flips = 0;
        var gate = new ManualResetEventSlim();

        ChessMatch Factory()
        {
            Interlocked.Increment(ref factoryRuns);
            // Cada sorteio devolve um valor diferente do anterior: se rodasse duas vezes, as cores divergiriam.
            var firstIsWhite = ColorAssignment.AssignFirst(ColorPreference.Random, ColorPreference.Random, () => Interlocked.Increment(ref flips) % 2 == 1) == PieceColor.White;
            var session = new ChessSession(TimeControl.Blitz, new ManualTime(), enableBackgroundTimer: false);
            session.SetSeat(0, "Ana", Guid.NewGuid(), firstIsWhite ? PieceColor.White : PieceColor.Black);
            session.SetSeat(1, "Bia", Guid.NewGuid(), firstIsWhite ? PieceColor.Black : PieceColor.White);
            return new ChessMatch(session);
        }

        Task<ChessMatch> Circuit(string connectionId, int seat) => Task.Run(() =>
        {
            gate.Wait();
            var match = registry.GetOrCreate(matchId, Factory);
            match.Seats[connectionId] = seat;
            return match;
        });

        var first = Circuit("c1", 0);
        var second = Circuit("c2", 1);
        gate.Set();
        var matches = await Task.WhenAll(first, second);

        Assert.Equal(1, Volatile.Read(ref factoryRuns));
        Assert.Same(matches[0], matches[1]);
        Assert.True(registry.TryGet(matchId, out var found));
        Assert.Same(matches[0], found);
        Assert.Equal(0, registry.SeatOf(matchId, "c1"));
        Assert.Equal(1, registry.SeatOf(matchId, "c2"));
        Assert.Null(registry.SeatOf(matchId, "desconhecida"));
        Assert.Null(registry.SeatOf(Guid.NewGuid(), "c1"));

        var session = matches[0].Session;
        var color1 = session.ColorOf(registry.SeatOf(matchId, "c1")!.Value);
        var color2 = session.ColorOf(registry.SeatOf(matchId, "c2")!.Value);
        Assert.NotEqual(color1, color2);
        Assert.Equal(color1, session.ColorOf(registry.SeatOf(matchId, "c1")!.Value));
    }

    [Fact(DisplayName = "SPEC-0053:UT-05 — Na revanche a cor de cada assento troca e SeatOf continua o mesmo")]
    [Trait("Category", "SPEC-0053:UT-05")]
    public void Rematch_ShouldSwapColorOfEachSeat()
    {
        var registry = new ChessMatchRegistry();
        var matchId = Guid.NewGuid();
        var session = ChessSessionTests.New();
        var match = registry.GetOrCreate(matchId, () => new ChessMatch(session));
        match.Seats["c1"] = 0;
        match.Seats["c2"] = 1;
        var before = session.ColorOf(registry.SeatOf(matchId, "c1")!.Value);
        ChessSessionLeaveTests.FoolsMate(session);

        Assert.True(session.RequestRematch(PieceColor.White));
        Assert.True(session.AcceptRematch(PieceColor.Black));

        Assert.Equal(0, registry.SeatOf(matchId, "c1"));
        Assert.NotEqual(before, session.ColorOf(registry.SeatOf(matchId, "c1")!.Value));
        Assert.NotEqual(session.ColorOf(0), session.ColorOf(1));
    }

    [Fact(DisplayName = "SPEC-0053:UT-05 — GetOrCreate devolve a partida existente sem rodar o factory; Remove apaga")]
    [Trait("Category", "SPEC-0053:UT-05")]
    public void GetOrCreate_WhenExists_ShouldReuseAndRemoveShouldDelete()
    {
        var registry = new ChessMatchRegistry();
        var matchId = Guid.NewGuid();
        var created = registry.GetOrCreate(matchId, () => new ChessMatch(ChessSessionTests.New()));

        var again = registry.GetOrCreate(matchId, () => throw new InvalidOperationException("factory não devia rodar"));

        Assert.Same(created, again);
        Assert.True(registry.Remove(matchId));
        Assert.False(registry.Remove(matchId));
        Assert.False(registry.TryGet(matchId, out _));
    }

    private sealed class TrackingTime : TimeProvider
    {
        public bool TimerDisposed { get; private set; }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) => new TrackingTimer(this);

        private sealed class TrackingTimer(TrackingTime owner) : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose() => owner.TimerDisposed = true;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    [Fact(DisplayName = "SPEC-0053:UT-05 — Factory que lança não fica cacheado: a próxima chamada com factory bom funciona")]
    [Trait("Category", "SPEC-0053:UT-05")]
    public void GetOrCreate_WhenFactoryThrows_ShouldNotCacheFailure()
    {
        var registry = new ChessMatchRegistry();
        var matchId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() => registry.GetOrCreate(matchId, () => throw new InvalidOperationException("falha")));
        Assert.False(registry.TryGet(matchId, out _));

        var match = registry.GetOrCreate(matchId, () => new ChessMatch(ChessSessionTests.New()));

        Assert.NotNull(match);
        Assert.True(registry.TryGet(matchId, out var found));
        Assert.Same(match, found);
    }

    [Fact(DisplayName = "SPEC-0053:UT-05 — Remove com o factory em execução espera e descarta a sessão criada")]
    [Trait("Category", "SPEC-0053:UT-05")]
    public async Task Remove_WhileFactoryRuns_ShouldDisposeCreatedSession()
    {
        var registry = new ChessMatchRegistry();
        var matchId = Guid.NewGuid();
        var time = new TrackingTime();
        var inFactory = new ManualResetEventSlim();
        var release = new ManualResetEventSlim();

        var creator = Task.Run(() => registry.GetOrCreate(matchId, () =>
        {
            inFactory.Set();
            release.Wait();
            return new ChessMatch(new ChessSession(TimeControl.Blitz, time));
        }));
        inFactory.Wait();
        var remover = Task.Run(() => registry.Remove(matchId));
        await Task.Delay(50);
        release.Set();

        Assert.True(await remover);
        await creator;
        Assert.True(time.TimerDisposed);
        Assert.False(registry.TryGet(matchId, out _));
    }
}
