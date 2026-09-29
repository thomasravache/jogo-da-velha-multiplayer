using System;
using System.Collections.Concurrent;

namespace TicTacToe.Modules.Matchmaking;

public class MatchmakingService
{
    private readonly ConcurrentQueue<string> _waitingPlayers = new();
    private readonly ConcurrentDictionary<string, string> _playerNames = new();
    private readonly ConcurrentDictionary<string, Guid> _playerIds = new();

    // playerConnectionId -> matchId
    public ConcurrentDictionary<string, Guid> ActiveMatches { get; } = new();

    // matchId -> (PlayerXConnectionId, PlayerOConnectionId)
    private readonly ConcurrentDictionary<Guid, (string PlayerX, string PlayerO)> _matchPlayers = new();

    public event Action<string, Guid>? OnPlayerMatched;

    public Guid? JoinQueue(string connectionId, string playerName = "", Guid? playerId = null, int bestOf = 1)
    {
        _ = bestOf;
        RememberId(connectionId, playerId);
        _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
        _waitingPlayers.Enqueue(connectionId);

        if (_waitingPlayers.Count >= 2)
        {
            if (_waitingPlayers.TryDequeue(out var player1) && _waitingPlayers.TryDequeue(out var player2))
            {
                var matchId = Guid.NewGuid();
                ActiveMatches[player1] = matchId;
                ActiveMatches[player2] = matchId;
                _matchPlayers[matchId] = (player1, player2);
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

    private readonly ConcurrentDictionary<string, string> _privateRooms = new();

    public string CreatePrivateRoom(string connectionId, string playerName, Guid? playerId = null, int bestOf = 1)
    {
        RememberId(connectionId, playerId);
        _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
        _ = bestOf;
        string code = "SALA-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        _privateRooms[code] = connectionId;
        return code;
    }

    public Guid? JoinPrivateRoom(string roomCode, string connectionId, string playerName, Guid? playerId = null)
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
            OnPlayerMatched?.Invoke(hostConnectionId, matchId);
            OnPlayerMatched?.Invoke(connectionId, matchId);
            return matchId;
        }

        return null;
    }
}
