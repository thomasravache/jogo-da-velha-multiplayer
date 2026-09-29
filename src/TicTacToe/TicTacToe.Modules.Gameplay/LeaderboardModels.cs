namespace TicTacToe.Modules.Gameplay;

public sealed record LeaderboardQuery(Guid? MyPlayerId, int Page, int PageSize = 10);

public sealed record LeaderboardEntry(
    int Position,
    string DisplayName,
    int Wins,
    int Losses,
    int Draws,
    double WinRatePercent,
    int WinStreak,
    DateTime LastWinAtUtc,
    bool IsMe);

public sealed record LeaderboardPage(
    IReadOnlyList<LeaderboardEntry> Items,
    int TotalPlayers,
    int Page,
    int PageCount,
    LeaderboardEntry? Me);

/// <summary>Projeção mínima de <see cref="MatchResult"/> usada pela classificação.</summary>
public sealed record LeaderboardGame(
    string PlayerXName,
    string PlayerOName,
    Guid? PlayerXId,
    Guid? PlayerOId,
    string? WinnerName,
    string? WinnerSide,
    GameMode? Mode,
    DateTime PlayedAtUtc);
