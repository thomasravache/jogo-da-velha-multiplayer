namespace TicTacToe.Modules.Chess;

public sealed class Position
{
    private Position()
    {
    }

    public static Position Start => throw new NotImplementedException();

    public PieceColor SideToMove => throw new NotImplementedException();

    public Square? EnPassantTarget => throw new NotImplementedException();

    public int HalfmoveClock => throw new NotImplementedException();

    public int FullmoveNumber => throw new NotImplementedException();

    public static Position FromFen(string fen) => throw new NotImplementedException();

    public static bool TryFromFen(string fen, out Position? position) => throw new NotImplementedException();

    public string ToFen() => throw new NotImplementedException();

    public Piece? PieceAt(Square square) => throw new NotImplementedException();

    public bool CanCastle(PieceColor color, bool kingSide) => throw new NotImplementedException();

    public IReadOnlyList<Move> LegalMoves() => throw new NotImplementedException();

    public bool IsInCheck(PieceColor color) => throw new NotImplementedException();

    public Position Apply(Move move) => throw new NotImplementedException();

    public bool IsLegal(Move move) => throw new NotImplementedException();
}
