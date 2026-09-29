namespace TicTacToe.Modules.Chess;

public enum ChessOutcome
{
    WhiteWins,
    BlackWins,
    Draw,
}

public enum ChessEndReason
{
    Checkmate,
    Stalemate,
    InsufficientMaterial,
    FiftyMoveRule,
    ThreefoldRepetition,
    Timeout,
    Resignation,
    Abandon,
    Disconnect,
}

public sealed record ChessResult(ChessOutcome Outcome, ChessEndReason Reason);
