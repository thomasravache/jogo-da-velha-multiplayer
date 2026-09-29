namespace TicTacToe.Modules.Chess;

/// <summary>Resultado de <see cref="ChessSession.Leave"/> (SPEC-0061).</summary>
public enum ChessLeaveResult
{
    Forfeited,
    Discarded,
    Left,
    Rejected,
}

/// <summary>Estado do pedido de revanche (SPEC-0061).</summary>
public enum ChessRematchState
{
    None,
    Requested,
    Declined,
    Expired,
}
