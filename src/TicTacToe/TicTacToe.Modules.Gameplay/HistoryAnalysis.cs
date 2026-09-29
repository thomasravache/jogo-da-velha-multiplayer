using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToe.Modules.Gameplay;

/// <summary>Regras puras do histórico (sem banco): classificação, texto do motivo, duração e resumo.</summary>
public static class HistoryAnalysis
{
    private const string Dash = "—";

    public static HistoryOutcome Classify(string? winnerSide, bool iAmX) =>
        winnerSide is null ? HistoryOutcome.Draw : (winnerSide == "X") == iAmX ? HistoryOutcome.Win : HistoryOutcome.Loss;

    public static string Reason(EndReason? reason, string? winningLine) => reason switch
    {
        EndReason.Line => LineReason(winningLine),
        EndReason.Draw => "Grid completo sem vencedor",
        EndReason.Timeout => "Tempo esgotado",
        EndReason.Abandon => "Abandono",
        EndReason.Disconnect => "Desconexão do oponente",
        _ => Dash,
    };

    public static bool IsWalkOver(EndReason? reason) =>
        reason is EndReason.Timeout or EndReason.Abandon or EndReason.Disconnect;

    public static string FormatDuration(int? seconds) => seconds switch
    {
        null => Dash,
        < 60 => $"{seconds}s",
        _ => $"{seconds / 60}m {seconds % 60:00}s",
    };

    public static PlayerSummary Summarize(IEnumerable<SummaryGame> games)
    {
        var ordered = games.OrderByDescending(g => g.PlayedAtUtc).ToList();
        if (ordered.Count == 0) return new PlayerSummary(0, 0, 0, 0, 0, 0, null, null);

        var outcomes = ordered.Select(g => Classify(g.WinnerSide, g.IAmX)).ToList();
        var wins = outcomes.Count(o => o == HistoryOutcome.Win);
        var losses = outcomes.Count(o => o == HistoryOutcome.Loss);
        var draws = outcomes.Count - wins - losses;
        var streak = outcomes.TakeWhile(o => o == HistoryOutcome.Win).Count();

        var lineWins = ordered.Where((g, i) => outcomes[i] == HistoryOutcome.Win && g.EndReason == EndReason.Line && g.DurationSeconds is not null)
            .Select(g => g.DurationSeconds!.Value).ToList();
        var perMove = ordered.Where(g => g.DurationSeconds is not null && g.MoveCount is > 0)
            .Select(g => (double)g.DurationSeconds!.Value / g.MoveCount!.Value).ToList();

        return new PlayerSummary(
            ordered.Count, wins, losses, draws,
            Math.Round(100.0 * wins / ordered.Count, 1, MidpointRounding.AwayFromZero),
            streak,
            lineWins.Count > 0 ? lineWins.Min() : null,
            perMove.Count > 0 ? perMove.Average() : null);
    }

    private static string LineReason(string? line) => line switch
    {
        "0,1,2" or "3,4,5" or "6,7,8" => "3 em linha horizontal",
        "0,3,6" or "1,4,7" or "2,5,8" => "3 em linha vertical",
        "0,4,8" => "3 em linha diagonal principal",
        "2,4,6" => "3 em linha diagonal secundária",
        _ => "3 em linha",
    };
}
