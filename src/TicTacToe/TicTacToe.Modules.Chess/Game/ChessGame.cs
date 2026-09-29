namespace TicTacToe.Modules.Chess;

/// <summary>Partida de xadrez mutável e não concorrente (o lock fica na sessão) sobre <see cref="Chess.Position"/> imutável.</summary>
public sealed class ChessGame
{
    public ChessGame(Position? start = null)
    {
        Position = start ?? Position.Start;
    }

    public Position Position { get; }

    public IReadOnlyList<ChessMove> Moves { get; } = [];

    public ChessResult? Result { get; }

    public bool IsOver => Result is not null;

    public string MovesSan => throw new NotImplementedException();

    public IReadOnlyList<Move> LegalMoves() => throw new NotImplementedException();

    public IReadOnlyList<Square> DestinationsFrom(Square from) => throw new NotImplementedException();

    public bool NeedsPromotion(Square from, Square to) => throw new NotImplementedException();

    public bool TryPlay(Square from, Square to, PieceType? promotion, out ChessMove? played) =>
        throw new NotImplementedException();

    public IReadOnlyList<PieceType> CapturedBy(PieceColor color) => throw new NotImplementedException();

    public bool HasMatingMaterial(PieceColor color) => throw new NotImplementedException();

    public void End(ChessResult result) => throw new NotImplementedException();
}
