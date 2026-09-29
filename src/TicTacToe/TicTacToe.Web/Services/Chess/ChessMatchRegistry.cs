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
    public ChessMatch GetOrCreate(Guid matchId, Func<ChessMatch> factory) => throw new NotImplementedException();

    public bool TryGet(Guid matchId, out ChessMatch match) => throw new NotImplementedException();

    public int? SeatOf(Guid matchId, string connectionId) => throw new NotImplementedException();

    public bool Remove(Guid matchId) => throw new NotImplementedException();
}
