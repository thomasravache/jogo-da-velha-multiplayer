namespace TicTacToe.Modules.Chess;

public sealed partial class ChessSession
{
    public const int DisconnectGraceSeconds = 15;

    public ChessRematchState RematchState => throw new NotImplementedException();

    public PieceColor? RematchRequestedBy => throw new NotImplementedException();

    public bool Forfeit(PieceColor loser, ChessEndReason reason) => throw new NotImplementedException();

    public ChessLeaveResult Leave(PieceColor player) => throw new NotImplementedException();

    public bool HasLeft(PieceColor color) => throw new NotImplementedException();

    public bool RequestRematch(PieceColor player) => throw new NotImplementedException();

    public bool AcceptRematch(PieceColor player) => throw new NotImplementedException();

    public bool DeclineRematch(PieceColor player) => throw new NotImplementedException();

    public void SetConnection(PieceColor player, bool connected) => throw new NotImplementedException();

    public int? DisconnectSecondsLeft(PieceColor player) => throw new NotImplementedException();
}
