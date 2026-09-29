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

    public ChessMatch GetOrCreate(Guid matchId, Func<ChessMatch> factory)
    {
        var lazy = _matches.GetOrAdd(matchId, _ => new Lazy<ChessMatch>(factory, LazyThreadSafetyMode.ExecutionAndPublication));
        try
        {
            return lazy.Value;
        }
        catch
        {
            // Não deixa o Lazy quebrado cacheado: a próxima chamada roda o factory de novo.
            _matches.TryRemove(KeyValuePair.Create(matchId, lazy));
            throw;
        }
    }

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

        // Forçar o valor espera um factory em execução, para não deixar a sessão órfã com o timer vivo.
        try
        {
            lazy.Value.Session.Dispose();
        }
        catch (Exception)
        {
            // Factory falhou: não há sessão a descartar.
        }

        return true;
    }
}
