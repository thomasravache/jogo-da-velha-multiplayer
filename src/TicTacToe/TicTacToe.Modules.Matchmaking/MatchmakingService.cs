using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TicTacToe.Modules.Matchmaking;

public class MatchmakingService
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, List<string>> _queues = [];
    private readonly Dictionary<string, (string QueueKey, int BestOf)> _roomInfo = [];
    private readonly ConcurrentDictionary<string, string> _playerNames = new();
    private readonly ConcurrentDictionary<string, Guid> _playerIds = new();

    // playerConnectionId -> matchId
    public ConcurrentDictionary<string, Guid> ActiveMatches { get; } = new();

    // matchId -> (PlayerXConnectionId, PlayerOConnectionId)
    private readonly ConcurrentDictionary<Guid, (string PlayerX, string PlayerO)> _matchPlayers = new();

    public event Action<string, Guid>? OnPlayerMatched;

    private static string EffectiveKey(string? queueKey, int bestOf) =>
        string.IsNullOrWhiteSpace(queueKey) ? $"velha:{bestOf}" : queueKey;

    public Guid? JoinQueue(string connectionId, string playerName = "", Guid? playerId = null, int bestOf = 1, string? queueKey = null)
    {
        RememberId(connectionId, playerId);
        _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
        var key = EffectiveKey(queueKey, bestOf);

        string player1, player2;
        Guid matchId;
        lock (_gate)
        {
            RemoveFromQueues(connectionId);
            if (!_queues.TryGetValue(key, out var queue))
            {
                queue = [];
                _queues[key] = queue;
            }

            queue.Add(connectionId);
            if (queue.Count < 2) return null;

            player1 = queue[0];
            player2 = queue[1];
            queue.RemoveRange(0, 2);
            matchId = Guid.NewGuid();
            ActiveMatches[player1] = matchId;
            ActiveMatches[player2] = matchId;
            _matchPlayers[matchId] = (player1, player2);
            _matchBestOf[matchId] = bestOf;
            _matchQueueKey[matchId] = key;
        }

        OnPlayerMatched?.Invoke(player1, matchId);
        OnPlayerMatched?.Invoke(player2, matchId);
        return matchId;
    }

    private void RemoveFromQueues(string connectionId)
    {
        foreach (var queue in _queues.Values)
        {
            queue.RemoveAll(c => c == connectionId);
        }
    }

    /// <summary>Remove a conexão de qualquer fila (idempotente); quem já foi pareado não é afetado.</summary>
    public void LeaveQueue(string connectionId)
    {
        lock (_gate) RemoveFromQueues(connectionId);
    }

    /// <summary>Remove as salas privadas criadas pela conexão que ainda esperam.</summary>
    public void CancelPrivateRoom(string connectionId)
    {
        lock (_gate)
        {
            foreach (var code in _privateRooms.Where(r => r.Value == connectionId).Select(r => r.Key).ToList())
            {
                _privateRooms.TryRemove(code, out _);
                _roomInfo.Remove(code);
            }
        }
    }

    private readonly ConcurrentDictionary<Guid, string> _matchQueueKey = new();

    /// <summary>Chave efetiva da fila que originou a partida (ex.: "velha:1").</summary>
    public string GetMatchQueueKey(Guid matchId) =>
        _matchQueueKey.TryGetValue(matchId, out var key) ? key : EffectiveKey(null, GetMatchBestOf(matchId));

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

    private readonly ConcurrentDictionary<string, string> _privateRooms = new();

    public string CreatePrivateRoom(string connectionId, string playerName, Guid? playerId = null, int bestOf = 1, string? queueKey = null)
    {
        RememberId(connectionId, playerId);
        _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
        string code = "SALA-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        lock (_gate)
        {
            _privateRooms[code] = connectionId;
            _roomInfo[code] = (EffectiveKey(queueKey, bestOf), bestOf);
        }

        return code;
    }

    public Guid? JoinPrivateRoom(string roomCode, string connectionId, string playerName, Guid? playerId = null, string game = "velha")
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return null;
        string normalized = roomCode.Trim().ToUpperInvariant();

        string hostConnectionId;
        Guid matchId;
        lock (_gate)
        {
            // Sala de outro jogo é tratada como inválida e continua esperando.
            if (!_roomInfo.TryGetValue(normalized, out var info)
                || !info.QueueKey.StartsWith(game + ":", StringComparison.Ordinal)
                || !_privateRooms.TryRemove(normalized, out hostConnectionId!))
            {
                return null;
            }

            _roomInfo.Remove(normalized);
            RememberId(connectionId, playerId);
            _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
            matchId = Guid.NewGuid();
            ActiveMatches[hostConnectionId] = matchId;
            ActiveMatches[connectionId] = matchId;
            _matchPlayers[matchId] = (hostConnectionId, connectionId);
            _privateMatches[matchId] = true;
            _matchBestOf[matchId] = info.BestOf;
            _matchQueueKey[matchId] = info.QueueKey;
        }

        OnPlayerMatched?.Invoke(hostConnectionId, matchId);
        OnPlayerMatched?.Invoke(connectionId, matchId);
        return matchId;
    }
}
