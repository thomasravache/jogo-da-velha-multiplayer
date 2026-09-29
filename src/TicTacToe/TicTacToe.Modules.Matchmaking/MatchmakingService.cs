using System;
using System.Collections.Concurrent;

namespace TicTacToe.Modules.Matchmaking;

public class MatchmakingService
{
    private readonly ConcurrentQueue<string> _waitingPlayers = new();
    private readonly ConcurrentDictionary<string, string> _playerNames = new();

    // playerConnectionId -> matchId
    public ConcurrentDictionary<string, Guid> ActiveMatches { get; } = new();

    public event Action<string, Guid>? OnPlayerMatched;

    public Guid? JoinQueue(string connectionId, string playerName = "")
    {
        _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
        _waitingPlayers.Enqueue(connectionId);

        if (_waitingPlayers.Count >= 2)
        {
            if (_waitingPlayers.TryDequeue(out var player1) && _waitingPlayers.TryDequeue(out var player2))
            {
                var matchId = Guid.NewGuid();
                ActiveMatches[player1] = matchId;
                ActiveMatches[player2] = matchId;
                OnPlayerMatched?.Invoke(player1, matchId);
                OnPlayerMatched?.Invoke(player2, matchId);
                return matchId;
            }
        }
        return null;
    }

    public string? GetPlayerName(string connectionId) =>
        _playerNames.TryGetValue(connectionId, out var name) ? name : null;

    private readonly ConcurrentDictionary<string, string> _privateRooms = new();

    public string CreatePrivateRoom(string connectionId, string playerName)
    {
        _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
        string code = "SALA-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        _privateRooms[code] = connectionId;
        return code;
    }

    public Guid? JoinPrivateRoom(string roomCode, string connectionId, string playerName)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return null;
        string normalized = roomCode.Trim().ToUpperInvariant();

        if (_privateRooms.TryRemove(normalized, out var hostConnectionId))
        {
            _playerNames[connectionId] = string.IsNullOrWhiteSpace(playerName) ? connectionId : playerName;
            var matchId = Guid.NewGuid();
            ActiveMatches[hostConnectionId] = matchId;
            ActiveMatches[connectionId] = matchId;
            OnPlayerMatched?.Invoke(hostConnectionId, matchId);
            OnPlayerMatched?.Invoke(connectionId, matchId);
            return matchId;
        }

        return null;
    }
}
