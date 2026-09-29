using System;
using System.Collections.Concurrent;

namespace TicTacToe.Modules.Matchmaking;

public class MatchmakingService
{
    private readonly ConcurrentDictionary<int, ConcurrentQueue<string>> _queues = new();
    private readonly ConcurrentDictionary<string, int> _roomBestOf = new();
    private readonly ConcurrentDictionary<string, string> _playerNames = new();
    private readonly ConcurrentDictionary<string, Guid> _playerIds = new();

    // playerConnectionId -> matchId
    public ConcurrentDictionary<string, Guid> ActiveMatches { get; } = new();

    // matchId -> (PlayerXConnectionId, PlayerOConnectionId)
    private readonly ConcurrentDictionary<Guid, (string PlayerX, string PlayerO)> _matchPlayers = new();

    public event Action<string, Guid>? OnPlayerMatched;

    public Guid? JoinQueue(string connectionId, string playerName = "", Guid? playerId = null, int bestOf = 1, string? queueKey = null)
    {
        RememberId(connectionId, playerId);
        _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
        var queue = _queues.GetOrAdd(bestOf, _ => new ConcurrentQueue<string>());
        queue.Enqueue(connectionId);

        if (queue.Count >= 2)
        {
            if (queue.TryDequeue(out var player1) && queue.TryDequeue(out var player2))
            {
                var matchId = Guid.NewGuid();
                ActiveMatches[player1] = matchId;
                ActiveMatches[player2] = matchId;
                _matchPlayers[matchId] = (player1, player2);
                _matchBestOf[matchId] = bestOf;
                OnPlayerMatched?.Invoke(player1, matchId);
                OnPlayerMatched?.Invoke(player2, matchId);
                return matchId;
            }
        }
        return null;
    }

    private readonly ConcurrentDictionary<Guid, bool> _privateMatches = new();

    /// <summary>Verdadeiro se a partida veio de uma sala privada (e não da fila pública).</summary>
    public bool IsPrivateMatch(Guid matchId) => _privateMatches.ContainsKey(matchId);

    private void RememberId(string connectionId, Guid? playerId)
    {
        if (playerId is { } id)
        {
            _playerIds[connectionId] = id;
        }
        else
        {
            _playerIds.TryRemove(connectionId, out _);
        }
    }

    /// <summary>Identidades anônimas (se informadas) dos jogadores X e O da partida.</summary>
    public (Guid? X, Guid? O)? GetMatchPlayerIds(Guid matchId)
    {
        if (!_matchPlayers.TryGetValue(matchId, out var pair)) return null;

        Guid? Id(string connectionId) => _playerIds.TryGetValue(connectionId, out var id) ? id : null;
        return (Id(pair.PlayerX), Id(pair.PlayerO));
    }

    public string? GetPlayerName(string connectionId) =>
        _playerNames.TryGetValue(connectionId, out var name) ? name : null;

    public (string PlayerX, string PlayerO)? GetMatchPlayers(Guid matchId) =>
        _matchPlayers.TryGetValue(matchId, out var pair) ? pair : null;

    public (string PlayerXName, string PlayerOName)? GetMatchPlayerNames(Guid matchId)
    {
        if (_matchPlayers.TryGetValue(matchId, out var pair))
        {
            var xName = GetPlayerName(pair.PlayerX) ?? "X";
            var oName = GetPlayerName(pair.PlayerO) ?? "O";
            return (xName, oName);
        }
        return null;
    }

    /// <summary>Formato da partida: 1 = partida única, 5 = melhor de 5.</summary>
    public int GetMatchBestOf(Guid matchId) => _matchBestOf.TryGetValue(matchId, out var bestOf) ? bestOf : 1;

    private readonly ConcurrentDictionary<Guid, int> _matchBestOf = new();

    /// <summary>Chave efetiva da fila que originou a partida (ex.: "velha:1").</summary>
    public string GetMatchQueueKey(Guid matchId) => throw new NotImplementedException();

    /// <summary>Remove a conexão de qualquer fila (idempotente).</summary>
    public void LeaveQueue(string connectionId) => throw new NotImplementedException();

    /// <summary>Remove as salas privadas criadas pela conexão que ainda esperam.</summary>
    public void CancelPrivateRoom(string connectionId) => throw new NotImplementedException();

    private readonly ConcurrentDictionary<string, string> _privateRooms = new();

    public string CreatePrivateRoom(string connectionId, string playerName, Guid? playerId = null, int bestOf = 1, string? queueKey = null)
    {
        RememberId(connectionId, playerId);
        _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
        string code = "SALA-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        _privateRooms[code] = connectionId;
        _roomBestOf[code] = bestOf;
        return code;
    }

    public Guid? JoinPrivateRoom(string roomCode, string connectionId, string playerName, Guid? playerId = null, string game = "velha")
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return null;
        string normalized = roomCode.Trim().ToUpperInvariant();

        if (_privateRooms.TryRemove(normalized, out var hostConnectionId))
        {
            RememberId(connectionId, playerId);
            _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
            var matchId = Guid.NewGuid();
            ActiveMatches[hostConnectionId] = matchId;
            ActiveMatches[connectionId] = matchId;
            _matchPlayers[matchId] = (hostConnectionId, connectionId);
            _privateMatches[matchId] = true;
            _matchBestOf[matchId] = _roomBestOf.TryRemove(normalized, out var bestOf) ? bestOf : 1;
            OnPlayerMatched?.Invoke(hostConnectionId, matchId);
            OnPlayerMatched?.Invoke(connectionId, matchId);
            return matchId;
        }

        return null;
    }
}
