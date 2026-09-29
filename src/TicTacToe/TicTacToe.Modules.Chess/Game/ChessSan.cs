using System.Text;

namespace TicTacToe.Modules.Chess;

/// <summary>Notação algébrica padrão (SAN) como função pura: a desambiguação só considera lances legais.</summary>
internal static class ChessSan
{
    public static string Write(Position position, Move move, bool isCheck, bool isCheckmate)
    {
        var piece = position.PieceAt(move.From)!.Value.Type;
        var sb = new StringBuilder(8);

        if (piece == PieceType.King && Math.Abs(move.To.File - move.From.File) == 2)
        {
            sb.Append(move.To.File > move.From.File ? "O-O" : "O-O-O");
        }
        else
        {
            var isCapture = IsCapture(position, move, piece);
            if (piece == PieceType.Pawn)
            {
                if (isCapture)
                {
                    sb.Append(FileLetter(move.From)).Append('x');
                }
            }
            else
            {
                sb.Append(PieceLetter(piece)).Append(Disambiguation(position, move, piece));
                if (isCapture)
                {
                    sb.Append('x');
                }
            }

            sb.Append(move.To.ToString());
            if (move.Promotion is { } promotion)
            {
                sb.Append('=').Append(PieceLetter(promotion));
            }
        }

        if (isCheckmate)
        {
            sb.Append('#');
        }
        else if (isCheck)
        {
            sb.Append('+');
        }

        return sb.ToString();
    }

    public static bool IsCapture(Position position, Move move, PieceType piece) =>
        position.PieceAt(move.To) is not null
        || (piece == PieceType.Pawn && move.From.File != move.To.File);

    private static string Disambiguation(Position position, Move move, PieceType piece)
    {
        var sameFile = false;
        var sameRank = false;
        var ambiguous = false;
        foreach (var other in position.LegalMoves())
        {
            if (other.To != move.To || other.From == move.From
                || position.PieceAt(other.From)!.Value.Type != piece)
            {
                continue;
            }

            ambiguous = true;
            sameFile |= other.From.File == move.From.File;
            sameRank |= other.From.Rank == move.From.Rank;
        }

        if (!ambiguous)
        {
            return string.Empty;
        }

        if (!sameFile)
        {
            return FileLetter(move.From).ToString();
        }

        return sameRank ? move.From.ToString() : (move.From.Rank + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static char FileLetter(Square square) => (char)('a' + square.File);

    private static char PieceLetter(PieceType type) => type switch
    {
        PieceType.Knight => 'N',
        PieceType.Bishop => 'B',
        PieceType.Rook => 'R',
        PieceType.Queen => 'Q',
        PieceType.King => 'K',
        _ => 'P',
    };
}
