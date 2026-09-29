namespace TicTacToe.Modules.Gameplay;

/// <summary>Regras puras do histórico (sem banco): classificação, texto do motivo, duração e resumo.</summary>
public static class HistoryAnalysis
{
    public static HistoryOutcome Classify(string? winnerSide, bool iAmX) => throw new NotImplementedException();

    public static string Reason(EndReason? reason, string? winningLine) => throw new NotImplementedException();

    public static bool IsWalkOver(EndReason? reason) => throw new NotImplementedException();

    public static string FormatDuration(int? seconds) => throw new NotImplementedException();

    public static PlayerSummary Summarize(IEnumerable<SummaryGame> games) => throw new NotImplementedException();
}
