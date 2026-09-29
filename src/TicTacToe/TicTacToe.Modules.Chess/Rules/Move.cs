namespace TicTacToe.Modules.Chess;

public readonly record struct Move(Square From, Square To, PieceType? Promotion = null);
