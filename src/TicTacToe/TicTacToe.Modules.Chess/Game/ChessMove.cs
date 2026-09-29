namespace TicTacToe.Modules.Chess;

public sealed record ChessMove(Move Move, string San, PieceType Piece, PieceType? Captured, bool IsCheck, bool IsCheckmate);
