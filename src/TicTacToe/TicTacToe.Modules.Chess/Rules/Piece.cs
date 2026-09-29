namespace TicTacToe.Modules.Chess;

public enum PieceColor
{
    White,
    Black,
}

public enum PieceType
{
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King,
}

public readonly record struct Piece(PieceColor Color, PieceType Type);
