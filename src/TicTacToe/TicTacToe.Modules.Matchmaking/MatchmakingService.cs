using System;
using System.Collections.Concurrent;

namespace TicTacToe.Modules.Matchmaking;

public class MatchmakingService
{
    private readonly ConcurrentQueue<string> _waitingPlayers = new();
    private readonly ConcurrentDictionary<string, string> _playerNames = new();

    // playerConnectionId -> matchId
    public ConcurrentDictionary<string, Guid> ActiveMatches { get; } = new();

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
                return matchId;
            }
        }
        return null;
    }

    public string? GetPlayerName(string connectionId) =>
        _playerNames.TryGetValue(connectionId, out var name) ? name : null;
}
