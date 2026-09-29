namespace TicTacToe.Modules.Chess;

/// <summary>Partida de xadrez mutável e não concorrente (o lock fica na sessão) sobre <see cref="Chess.Position"/> imutável.</summary>
public sealed class ChessGame
{
    private readonly List<ChessMove> _moves = [];
    private readonly List<PieceType> _capturedByWhite = [];
    private readonly List<PieceType> _capturedByBlack = [];
    private readonly Dictionary<string, int> _occurrences = [];

    public ChessGame(Position? start = null)
    {
        Position = start ?? Position.Start;
        _occurrences[PositionKey(Position)] = 1;
    }

    public Position Position { get; private set; }

    public IReadOnlyList<ChessMove> Moves => _moves;

    public ChessResult? Result { get; private set; }

    public bool IsOver => Result is not null;

    public string MovesSan => string.Join(' ', _moves.Select(m => m.San));

    public IReadOnlyList<Move> LegalMoves() => Position.LegalMoves();

    public IReadOnlyList<Square> DestinationsFrom(Square from) =>
        [.. Position.LegalMoves().Where(m => m.From == from).Select(m => m.To).Distinct()];

    public bool NeedsPromotion(Square from, Square to) =>
        Position.LegalMoves().Any(m => m.From == from && m.To == to && m.Promotion is not null);

    public bool TryPlay(Square from, Square to, PieceType? promotion, out ChessMove? played)
    {
        played = null;
        var move = new Move(from, to, promotion);
        if (IsOver || !Position.IsLegal(move))
        {
            return false;
        }

        var mover = Position.SideToMove;
        var piece = Position.PieceAt(from)!.Value.Type;
        var isCapture = ChessSan.IsCapture(Position, move, piece);
        var captured = isCapture ? Position.PieceAt(to)?.Type ?? PieceType.Pawn : (PieceType?)null;

        var next = Position.Apply(move);
        var isCheck = next.IsInCheck(next.SideToMove);
        var hasReply = next.LegalMoves().Count > 0;
        var isCheckmate = isCheck && !hasReply;
        var san = ChessSan.Write(Position, move, isCheck, isCheckmate);

        played = new ChessMove(move, san, piece, captured, isCheck, isCheckmate);
        _moves.Add(played);
        if (captured is { } taken)
        {
            (mover == PieceColor.White ? _capturedByWhite : _capturedByBlack).Add(taken);
        }

        Position = next;
        var key = PositionKey(next);
        _occurrences[key] = _occurrences.GetValueOrDefault(key) + 1;
        Result = Evaluate(mover, isCheckmate, hasReply, _occurrences[key]);
        return true;
    }

    public IReadOnlyList<PieceType> CapturedBy(PieceColor color) =>
        [.. color == PieceColor.White ? _capturedByWhite : _capturedByBlack];

    public bool HasMatingMaterial(PieceColor color) => ChessMaterial.HasMatingMaterial(Position, color);

    public void End(ChessResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Result ??= result;
    }

    private ChessResult? Evaluate(PieceColor mover, bool isCheckmate, bool hasReply, int occurrences)
    {
        if (isCheckmate)
        {
            return new ChessResult(
                mover == PieceColor.White ? ChessOutcome.WhiteWins : ChessOutcome.BlackWins,
                ChessEndReason.Checkmate);
        }

        if (!hasReply)
        {
            return new ChessResult(ChessOutcome.Draw, ChessEndReason.Stalemate);
        }

        if (ChessMaterial.IsInsufficient(Position))
        {
            return new ChessResult(ChessOutcome.Draw, ChessEndReason.InsufficientMaterial);
        }

        if (Position.HalfmoveClock >= 100)
        {
            return new ChessResult(ChessOutcome.Draw, ChessEndReason.FiftyMoveRule);
        }

        return occurrences >= 3
            ? new ChessResult(ChessOutcome.Draw, ChessEndReason.ThreefoldRepetition)
            : null;
    }

    /// <summary>Peças, lado a jogar, direitos de roque e en passant (só se houver captura en passant legal).</summary>
    private static string PositionKey(Position position)
    {
        var fen = position.ToFen();
        var end = 0;
        for (var spaces = 0; end < fen.Length && spaces < 3; end++)
        {
            if (fen[end] == ' ')
            {
                spaces++;
            }
        }

        var key = fen[..end];
        return HasLegalEnPassant(position) ? key + position.EnPassantTarget : key;
    }

    private static bool HasLegalEnPassant(Position position)
    {
        if (position.EnPassantTarget is not { } target)
        {
            return false;
        }

        foreach (var move in position.LegalMoves())
        {
            if (move.To == target
                && move.From.File != target.File
                && position.PieceAt(move.From)!.Value.Type == PieceType.Pawn)
            {
                return true;
            }
        }

        return false;
    }
}
