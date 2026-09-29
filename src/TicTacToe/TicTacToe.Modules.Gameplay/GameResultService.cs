using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
                SeriesId = game.SeriesId,
                RoundNumber = game.Format == SeriesFormat.BestOf5 ? (game.Winner == Player.None ? game.RoundNumber : game.RoundNumber - 1) : null,
                BestOf = game.Format == SeriesFormat.BestOf5 ? 5 : null,
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

    public virtual async Task<HistoryPage> GetHistoryAsync(HistoryQuery query)
    {
        var mine = query.Scope == HistoryScope.Mine;
        if (mine && query.PlayerId is null)
        {
            return new HistoryPage([], 0, 1, 1, new HistoryCounts(0, 0, 0, 0, 0));
        }

        var me = query.PlayerId;
        IQueryable<MatchResult> scope = mine
            ? db.MatchResults.Where(m => m.PlayerXId == me || m.PlayerOId == me)
            : db.MatchResults;

        var term = query.Opponent?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            // LIKE parametrizado; a colação do banco decide a caixa (SQL Server: insensível por padrão).
            var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[") + "%";
            scope = mine
                ? scope.Where(m => m.PlayerXId == me ? EF.Functions.Like(m.PlayerOName, pattern, "\\") : EF.Functions.Like(m.PlayerXName, pattern, "\\"))
                : scope.Where(m => EF.Functions.Like(m.PlayerXName, pattern, "\\") || EF.Functions.Like(m.PlayerOName, pattern, "\\"));
        }

        // Contagens: escopo + busca, nunca o filtro nem a página (SPEC-0038, emenda v3).
        var counts = new HistoryCounts(
            await scope.CountAsync(),
            mine ? await scope.Where(WinPredicate(me)).CountAsync() : 0,
            mine ? await scope.Where(LossPredicate(me)).CountAsync() : 0,
            await scope.Where(DrawPredicate(mine)).CountAsync(),
            await scope.Where(WalkOverPredicate).CountAsync());

        var filtered = query.Filter switch
        {
            HistoryFilter.Wins when mine => scope.Where(WinPredicate(me)),
            HistoryFilter.Losses when mine => scope.Where(LossPredicate(me)),
            HistoryFilter.Draws => scope.Where(DrawPredicate(mine)),
            HistoryFilter.WalkOvers => scope.Where(WalkOverPredicate),
            _ => scope,
        };

        var total = await filtered.CountAsync();
        var pageSize = Math.Max(1, query.PageSize);
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var page = Math.Clamp(query.Page, 1, pageCount);

        var ordered = query.Sort switch
        {
            HistorySort.ShortestDuration => filtered
                .OrderBy(m => m.DurationSeconds == null).ThenBy(m => m.DurationSeconds).ThenByDescending(m => m.PlayedAt),
            HistorySort.Result when mine => filtered
                .OrderBy(m => ((m.PlayerXId == me && m.WinnerSide == "X") || (m.PlayerXId != me && m.WinnerSide == "O")) ? 0 : m.WinnerSide == null ? 1 : 2)
                .ThenByDescending(m => m.PlayedAt),
            HistorySort.Result => filtered
                .OrderBy(m => m.WinnerName == null ? 1 : 0).ThenByDescending(m => m.PlayedAt),
            _ => filtered.OrderByDescending(m => m.PlayedAt),
        };

        var rows = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var items = rows.Select(m => ToItem(m, mine ? me : null)).ToList();
        return new HistoryPage(items, total, page, pageCount, counts);
    }

    public virtual async Task<PlayerSummary> GetPlayerSummaryAsync(Guid playerId)
    {
        var rows = await db.MatchResults
            .Where(m => m.PlayerXId == playerId || m.PlayerOId == playerId)
            .Select(m => new { IAmX = m.PlayerXId == playerId, m.WinnerSide, m.EndReason, m.DurationSeconds, m.MoveCount, m.PlayedAt })
            .ToListAsync();

        return HistoryAnalysis.Summarize(rows.Select(r =>
            new SummaryGame(r.IAmX, r.WinnerSide, r.EndReason, r.DurationSeconds, r.MoveCount, r.PlayedAt)));
    }

    // Só lógica booleana e comparações simples: traduz para SQL Server sem CASE de bit aninhado.
    private static Expression<Func<MatchResult, bool>> WinPredicate(Guid? me) =>
        m => (m.PlayerXId == me && m.WinnerSide == "X") || (m.PlayerXId != me && m.WinnerSide == "O");

    private static Expression<Func<MatchResult, bool>> LossPredicate(Guid? me) =>
        m => (m.PlayerXId == me && m.WinnerSide == "O") || (m.PlayerXId != me && m.WinnerSide == "X");

    // Escopo pessoal decide pelo lado (como HistoryAnalysis.Classify); o global também olha o nome (partidas antigas).
    private static Expression<Func<MatchResult, bool>> DrawPredicate(bool mine) =>
        mine ? m => m.WinnerSide == null : m => m.WinnerSide == null && m.WinnerName == null;

    private static readonly Expression<Func<MatchResult, bool>> WalkOverPredicate =
        m => m.EndReason == EndReason.Timeout || m.EndReason == EndReason.Abandon || m.EndReason == EndReason.Disconnect;

    private static HistoryItem ToItem(MatchResult m, Guid? me)
    {
        // Partidas antigas não têm WinnerSide: deduz pelos nomes, exceto com homônimos (lado indeterminável).
        var side = m.WinnerSide ?? (m.WinnerName is null || m.PlayerXName == m.PlayerOName
            ? null
            : m.WinnerName == m.PlayerXName ? "X" : m.WinnerName == m.PlayerOName ? "O" : null);
        bool? iAmX = me is null ? null : m.PlayerXId == me;
        HistoryOutcome? outcome = iAmX is { } x ? HistoryAnalysis.Classify(m.WinnerSide, x) : null;

        return new HistoryItem(
            m.Id, m.PlayerXName, m.PlayerOName, outcome,
            HistoryAnalysis.IsWalkOver(m.EndReason),
            HistoryAnalysis.Reason(m.EndReason, m.WinningLine),
            m.DurationSeconds, m.Mode, m.PlayedAt, iAmX, side, m.WinnerName);
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
