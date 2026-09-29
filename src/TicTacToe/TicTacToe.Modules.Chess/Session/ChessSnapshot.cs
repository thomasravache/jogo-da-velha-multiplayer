namespace TicTacToe.Modules.Chess;

/// <summary>Instantâneo imutável da sessão, tirado sob lock; é o que a interface lê.</summary>
public sealed record ChessSnapshot(
    Guid SessionId,
    ChessMode Mode,
    TimeControl Control,
    Position Position,
    IReadOnlyList<ChessMove> Moves,
    IReadOnlyList<PieceType> CapturedByWhite,
    IReadOnlyList<PieceType> CapturedByBlack,
    TimeSpan WhiteRemaining,
    TimeSpan BlackRemaining,
    PieceColor? ClockRunning,
    PieceColor SideToMove,
    ChessResult? Result,
    string WhiteName,
    string BlackName,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc);
