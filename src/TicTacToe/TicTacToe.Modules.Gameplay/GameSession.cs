using System;
using System.Collections.Generic;

namespace TicTacToe.Modules.Gameplay;

public enum Player { None, X, O }

public class GameSession
{
    public Guid Id { get; } = Guid.NewGuid();

    private readonly Dictionary<Player, string> _playerNames = new();
    private readonly Dictionary<Player, int> _scores = new();

    public void SetPlayerName(Player player, string name) =>
        _playerNames[player] = name;

    public string GetPlayerName(Player player) =>
        _playerNames.TryGetValue(player, out var name) ? name : player.ToString();

    public int GetScore(Player player) =>
        _scores.TryGetValue(player, out var score) ? score : 0;

    public void ResetScores() => _scores.Clear();

    public Player[] Board { get; } = new Player[9];
    public Player CurrentTurn { get; private set; } = Player.X;
    public Player Winner { get; private set; } = Player.None;
    public bool IsDraw => Winner == Player.None && Array.TrueForAll(Board, p => p != Player.None);

    public event Action? OnStateChanged;

    public void Restart()
    {
        Array.Clear(Board, 0, Board.Length);
        Winner = Player.None;
        CurrentTurn = Player.X;
        OnStateChanged?.Invoke();
    }

    public bool MakeMove(int index, Player player)
    {
        if (Winner != Player.None || index < 0 || index > 8 || Board[index] != Player.None || CurrentTurn != player)
            return false;

        Board[index] = player;

        if (CheckWin(player))
        {
            Winner = player;
            _scores[player] = GetScore(player) + 1;
        }
        else
        {
            CurrentTurn = player == Player.X ? Player.O : Player.X;
        }
        OnStateChanged?.Invoke();
        return true;
    }

    private bool CheckWin(Player player)
    {
        int[][] winLines = new int[][]
        {
            new[] {0, 1, 2}, new[] {3, 4, 5}, new[] {6, 7, 8}, // Rows
            new[] {0, 3, 6}, new[] {1, 4, 7}, new[] {2, 5, 8}, // Cols
            new[] {0, 4, 8}, new[] {2, 4, 6}                   // Diags
        };

        foreach (var line in winLines)
        {
            if (Board[line[0]] == player && Board[line[1]] == player && Board[line[2]] == player)
                return true;
        }
        return false;
    }
}
