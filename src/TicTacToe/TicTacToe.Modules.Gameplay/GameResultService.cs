using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TicTacToe.Modules.Gameplay;

public record PlayerRank(string PlayerName, int Wins, DateTime LastWinAt);

public class GameResultService(GameplayDbContext db, ILogger<GameResultService> logger)
{
    public async Task SaveResultAsync(GameSession game)
    {
        try
        {
            var result = new MatchResult
            {
                PlayerXName = game.GetPlayerName(Player.X),
                PlayerOName = game.GetPlayerName(Player.O),
                WinnerName = game.Winner != Player.None
                    ? game.GetPlayerName(game.Winner)
                    : null,
                DurationSeconds = game.Duration is { } duration ? (int)Math.Round(duration.TotalSeconds) : null,
                MoveCount = game.MoveCount,
                EndReason = game.EndReason,
                WinnerSide = game.Winner == Player.None ? null : game.Winner.ToString(),
                WinningLine = game.WinningLine is { } line ? string.Join(',', line) : null,
                FinalBoard = game.FinalBoard,
                Mode = game.Mode,
                PlayerXId = game.GetPlayerId(Player.X),
                PlayerOId = game.GetPlayerId(Player.O),
            };

            db.MatchResults.Add(result);
            await db.SaveChangesAsync();
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Falha ao salvar resultado da partida. A partida continuou normalmente.");
        }
    }

    /// <summary>Grava o resultado uma única vez por rodada, independente de quantos circuitos observam a partida.</summary>
    public async Task<bool> SaveOnceAsync(GameSession game)
    {
        if (game.Winner == Player.None && !game.IsDraw) return false;
        if (!game.TryMarkResultRecorded())
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Resultado da partida {GameId} já gravado; gravação ignorada.", game.Id);
            }
            return false;
        }

        await SaveResultAsync(game);
        return true;
    }

    public async Task<List<MatchResult>> GetRecentAsync(int count = 10) =>
        await db.MatchResults
            .OrderByDescending(m => m.PlayedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<PlayerRank>> GetLeaderboardAsync(int top = 10)
    {
        var rawWins = await db.MatchResults
            .Where(m => m.WinnerName != null)
            .Select(m => new { WinnerName = m.WinnerName!, m.PlayedAt })
            .ToListAsync();

        return rawWins
            .GroupBy(m => m.WinnerName)
            .Select(g => new PlayerRank(
                g.Key,
                g.Count(),
                g.Max(m => m.PlayedAt)
            ))
            .OrderByDescending(r => r.Wins)
            .ThenByDescending(r => r.LastWinAt)
            .Take(top)
            .ToList();
    }
}
