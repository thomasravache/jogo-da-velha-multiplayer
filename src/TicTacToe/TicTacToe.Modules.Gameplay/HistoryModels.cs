namespace TicTacToe.Modules.Gameplay;

public enum HistoryScope { Mine, All }

/// <summary>WalkOvers = motivo de fim Timeout, Abandon ou Disconnect.</summary>
public enum HistoryFilter { All, Wins, Losses, Draws, WalkOvers }

/// <summary>Result: vitória, empate, derrota; depois mais recentes.</summary>
public enum HistorySort { Recent, ShortestDuration, Result }

public enum HistoryOutcome { Win, Loss, Draw }

public sealed record HistoryQuery(
    Guid? PlayerId,
    HistoryScope Scope,
    HistoryFilter Filter,
    string? Opponent,
    HistorySort Sort,
    int Page,
    int PageSize = 10);

public sealed record HistoryItem(
    Guid Id,
    string PlayerXName,
    string PlayerOName,
    HistoryOutcome? Outcome,
    bool WalkOver,
    string Reason,
    int? DurationSeconds,
    GameMode? Mode,
    DateTime PlayedAtUtc,
    bool? IAmX,
    string? WinnerSide);

public sealed record HistoryCounts(int All, int Wins, int Losses, int Draws, int WalkOvers);

public sealed record HistoryPage(IReadOnlyList<HistoryItem> Items, int TotalItems, int Page, int PageCount, HistoryCounts Counts);

public sealed record PlayerSummary(
    int Total,
    int Wins,
    int Losses,
    int Draws,
    double WinRatePercent,
    int CurrentWinStreak,
    int? FastestWinSeconds,
    double? AverageSecondsPerMove);

/// <summary>Dados mínimos de uma partida do ponto de vista de um jogador, para o resumo.</summary>
public sealed record SummaryGame(bool IAmX, string? WinnerSide, EndReason? EndReason, int? DurationSeconds, int? MoveCount, DateTime PlayedAtUtc);
