using System;
using System.Collections.Generic;

namespace TicTacToe.Modules.Gameplay;

public enum AiDifficulty
{
    Easy,
    Hard
}

public static class AiPlayer
{
    private static readonly Random _random = new();

    private static readonly int[][] WinLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    public static int GetBestMove(GameSession game, Player aiPlayer, AiDifficulty difficulty)
    {
        var freeCells = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (game.Board[i] == Player.None)
                freeCells.Add(i);
        }

        if (freeCells.Count == 0) return -1;

        if (difficulty == AiDifficulty.Easy)
        {
            return freeCells[_random.Next(freeCells.Count)];
        }

        Player opponent = aiPlayer == Player.X ? Player.O : Player.X;
        int bestScore = int.MinValue;
        int bestMove = freeCells[0];

        var board = (Player[])game.Board.Clone();

        foreach (int move in freeCells)
        {
            board[move] = aiPlayer;
            int score = Minimax(board, 0, false, aiPlayer, opponent);
            board[move] = Player.None;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private static int Minimax(Player[] board, int depth, bool isMaximizing, Player aiPlayer, Player opponent)
    {
        if (CheckWin(board, aiPlayer)) return 10 - depth;
        if (CheckWin(board, opponent)) return depth - 10;
        if (IsFull(board)) return 0;

        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == Player.None)
                {
                    board[i] = aiPlayer;
                    int eval = Minimax(board, depth + 1, false, aiPlayer, opponent);
                    board[i] = Player.None;
                    maxEval = Math.Max(maxEval, eval);
                }
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == Player.None)
                {
                    board[i] = opponent;
                    int eval = Minimax(board, depth + 1, true, aiPlayer, opponent);
                    board[i] = Player.None;
                    minEval = Math.Min(minEval, eval);
                }
            }
            return minEval;
        }
    }

    private static bool CheckWin(Player[] board, Player player)
    {
        foreach (var line in WinLines)
        {
            if (board[line[0]] == player && board[line[1]] == player && board[line[2]] == player)
                return true;
        }
        return false;
    }

    private static bool IsFull(Player[] board)
    {
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == Player.None) return false;
        }
        return true;
    }
}
