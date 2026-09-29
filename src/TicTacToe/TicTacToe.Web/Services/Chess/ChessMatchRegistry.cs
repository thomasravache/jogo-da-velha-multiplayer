using System.Collections.Concurrent;
using TicTacToe.Modules.Chess;

namespace TicTacToe.Web.Services.Chess;

/// <summary>Sessão de uma partida de xadrez e a associação connectionId → assento (0|1).</summary>
public sealed class ChessMatch(ChessSession session)
{
    public ChessSession Session { get; } = session;

    public ConcurrentDictionary<string, int> Seats { get; } = new();
}

/// <summary>Registro singleton das partidas de xadrez em andamento (SPEC-0053).</summary>
public sealed class ChessMatchRegistry
{
    // Lazy garante que o factory (e o sorteio de cores dentro dele) rode uma única vez por partida.
    private readonly ConcurrentDictionary<Guid, Lazy<ChessMatch>> _matches = new();

    public ChessMatch GetOrCreate(Guid matchId, Func<ChessMatch> factory) =>
        _matches.GetOrAdd(matchId, _ => new Lazy<ChessMatch>(factory, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    public bool TryGet(Guid matchId, out ChessMatch match)
    {
        if (_matches.TryGetValue(matchId, out var lazy) && lazy.IsValueCreated)
        {
            match = lazy.Value;
            return true;
        }

        match = null!;
        return false;
    }

    public int? SeatOf(Guid matchId, string connectionId) =>
        TryGet(matchId, out var match) && match.Seats.TryGetValue(connectionId, out var seat) ? seat : null;

    public bool Remove(Guid matchId)
    {
        if (!_matches.TryRemove(matchId, out var lazy))
        {
            return false;
        }

        if (lazy.IsValueCreated)
        {
            lazy.Value.Session.Dispose();
        }

        return true;
    }
}
