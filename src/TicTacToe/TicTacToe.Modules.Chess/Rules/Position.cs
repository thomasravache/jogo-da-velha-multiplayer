using System.Globalization;
using System.Text;

namespace TicTacToe.Modules.Chess;

/// <summary>
/// Posição de xadrez imutável. Tabuleiro em array de 64 casas (a1 = 0 … h8 = 63);
/// peças codificadas como 0 = vazio, 1..6 = brancas (peão..rei), 7..12 = pretas.
/// </summary>
public sealed class Position
{
    private const int WhiteKingSide = 1;
    private const int WhiteQueenSide = 2;
    private const int BlackKingSide = 4;
    private const int BlackQueenSide = 8;

    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string PieceLetters = "PNBRQKpnbrqk";

    private static readonly int[] KnightFiles = [1, 2, 2, 1, -1, -2, -2, -1];
    private static readonly int[] KnightRanks = [2, 1, -1, -2, -2, -1, 1, 2];
    private static readonly int[] KingFiles = [1, 1, 1, 0, -1, -1, -1, 0];
    private static readonly int[] KingRanks = [1, 0, -1, -1, -1, 0, 1, 1];
    private static readonly int[] BishopFiles = [1, 1, -1, -1];
    private static readonly int[] BishopRanks = [1, -1, 1, -1];
    private static readonly int[] RookFiles = [1, -1, 0, 0];
    private static readonly int[] RookRanks = [0, 0, 1, -1];
    private static readonly PieceType[] PromotionTypes = [PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight];

    private static readonly Position StartPosition = Parse(StartFen)!;

    private readonly byte[] _board;
    private readonly bool _blackToMove;
    private readonly int _castling;
    private readonly int _enPassant;
    private readonly int _whiteKing;
    private readonly int _blackKing;
    private IReadOnlyList<Move>? _legalMoves;

    private Position(byte[] board, bool blackToMove, int castling, int enPassant, int halfmoveClock, int fullmoveNumber)
    {
        _board = board;
        _blackToMove = blackToMove;
        _castling = castling;
        _enPassant = enPassant;
        HalfmoveClock = halfmoveClock;
        FullmoveNumber = fullmoveNumber;
        _whiteKing = Array.IndexOf(board, (byte)6);
        _blackKing = Array.IndexOf(board, (byte)12);
    }

    public static Position Start => StartPosition;

    public PieceColor SideToMove => _blackToMove ? PieceColor.Black : PieceColor.White;

    public Square? EnPassantTarget => _enPassant < 0 ? null : new Square(_enPassant);

    public int HalfmoveClock { get; }

    public int FullmoveNumber { get; }

    public static Position FromFen(string fen) =>
        Parse(fen) ?? throw new FormatException($"FEN inválido: '{fen}'.");

    public static bool TryFromFen(string fen, out Position? position)
    {
        position = Parse(fen);
        return position is not null;
    }

    public string ToFen()
    {
        var sb = new StringBuilder(80);
        for (var rank = 7; rank >= 0; rank--)
        {
            var empty = 0;
            for (var file = 0; file < 8; file++)
            {
                var code = _board[(rank * 8) + file];
                if (code == 0)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    sb.Append((char)('0' + empty));
                    empty = 0;
                }

                sb.Append(PieceLetters[code - 1]);
            }

            if (empty > 0)
            {
                sb.Append((char)('0' + empty));
            }

            if (rank > 0)
            {
                sb.Append('/');
            }
        }

        sb.Append(_blackToMove ? " b " : " w ");
        if (_castling == 0)
        {
            sb.Append('-');
        }
        else
        {
            AppendIf(sb, WhiteKingSide, 'K');
            AppendIf(sb, WhiteQueenSide, 'Q');
            AppendIf(sb, BlackKingSide, 'k');
            AppendIf(sb, BlackQueenSide, 'q');
        }

        sb.Append(' ');
        sb.Append(_enPassant < 0 ? "-" : new Square(_enPassant).ToString());
        sb.Append(CultureInfo.InvariantCulture, $" {HalfmoveClock} {FullmoveNumber}");
        return sb.ToString();

        void AppendIf(StringBuilder builder, int flag, char letter)
        {
            if ((_castling & flag) != 0)
            {
                builder.Append(letter);
            }
        }
    }

    public Piece? PieceAt(Square square)
    {
        if (square.Index is < 0 or > 63)
        {
            return null;
        }

        var code = _board[square.Index];
        return code == 0 ? null : Decode(code);
    }

    public bool CanCastle(PieceColor color, bool kingSide) =>
        (_castling & (color == PieceColor.White
            ? (kingSide ? WhiteKingSide : WhiteQueenSide)
            : (kingSide ? BlackKingSide : BlackQueenSide))) != 0;

    public IReadOnlyList<Move> LegalMoves()
    {
        var cached = _legalMoves;
        if (cached is not null)
        {
            return cached;
        }

        var list = new List<Move>(48);
        GenerateLegal(list);
        var frozen = list.ToArray(); // o chamador não pode alterar o cache da posição
        _legalMoves = frozen;
        return frozen;
    }

    public bool IsInCheck(PieceColor color)
    {
        var black = color == PieceColor.Black;
        return IsAttacked(_board, black ? _blackKing : _whiteKing, !black);
    }

    public bool IsLegal(Move move) => LegalMoves().Contains(move);

    public Position Apply(Move move)
    {
        if (!IsLegal(move))
        {
            throw new ArgumentException($"Lance ilegal: {move.From}{move.To}.", nameof(move));
        }

        return ApplyUnchecked(move);
    }

    internal long PerftCount(int depth)
    {
        if (depth == 0)
        {
            return 1;
        }

        var moves = new List<Move>(48);
        GenerateLegal(moves);
        if (depth == 1)
        {
            return moves.Count;
        }

        long total = 0;
        foreach (var move in moves)
        {
            total += ApplyUnchecked(move).PerftCount(depth - 1);
        }

        return total;
    }

    // ---------- FEN ----------

    private static Position? Parse(string? fen)
    {
        if (string.IsNullOrEmpty(fen))
        {
            return null;
        }

        var fields = fen.Split(' ');
        if (fields.Length != 6)
        {
            return null;
        }

        var board = ParseBoard(fields[0]);
        if (board is null)
        {
            return null;
        }

        bool blackToMove;
        switch (fields[1])
        {
            case "w":
                blackToMove = false;
                break;
            case "b":
                blackToMove = true;
                break;
            default:
                return null;
        }

        var castling = ParseCastling(fields[2], board);
        var enPassant = ParseEnPassant(fields[3], blackToMove, board);
        if (castling < 0 || enPassant < -1)
        {
            return null;
        }

        if (!TryParseClock(fields[4], out var half) || half < 0
            || !TryParseClock(fields[5], out var full) || full < 1)
        {
            return null;
        }

        var position = new Position(board, blackToMove, castling, enPassant, half, full);

        // O lado que não joga não pode estar com o rei em xeque.
        var waiting = blackToMove ? position._whiteKing : position._blackKing;
        return IsAttacked(board, waiting, blackToMove) ? null : position;
    }

    private static bool TryParseClock(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static byte[]? ParseBoard(string text)
    {
        var ranks = text.Split('/');
        if (ranks.Length != 8)
        {
            return null;
        }

        var board = new byte[64];
        var kings = 0;
        var blackKings = 0;
        for (var i = 0; i < 8; i++)
        {
            var rank = 7 - i;
            var file = 0;
            foreach (var ch in ranks[i])
            {
                if (ch is >= '1' and <= '8')
                {
                    file += ch - '0';
                    continue;
                }

                var idx = PieceLetters.IndexOf(ch, StringComparison.Ordinal);
                if (idx < 0 || file >= 8)
                {
                    return null;
                }

                var code = (byte)(idx + 1);
                if (code is 1 or 7 && rank is 0 or 7)
                {
                    return null;
                }

                if (code == 6)
                {
                    kings++;
                }
                else if (code == 12)
                {
                    blackKings++;
                }

                board[(rank * 8) + file] = code;
                file++;
            }

            if (file != 8)
            {
                return null;
            }
        }

        return kings == 1 && blackKings == 1 ? board : null;
    }

    private static int ParseCastling(string text, byte[] board)
    {
        if (text == "-")
        {
            return 0;
        }

        if (text.Length == 0)
        {
            return -1; // campo vazio é inválido
        }

        var rights = 0;
        foreach (var ch in text)
        {
            var flag = ch switch
            {
                'K' => WhiteKingSide,
                'Q' => WhiteQueenSide,
                'k' => BlackKingSide,
                'q' => BlackQueenSide,
                _ => 0,
            };

            if (flag == 0 || (rights & flag) != 0)
            {
                return -1;
            }

            rights |= flag;
        }

        var valid = ((rights & WhiteKingSide) == 0 || (board[4] == 6 && board[7] == 4))
            && ((rights & WhiteQueenSide) == 0 || (board[4] == 6 && board[0] == 4))
            && ((rights & BlackKingSide) == 0 || (board[60] == 12 && board[63] == 10))
            && ((rights & BlackQueenSide) == 0 || (board[60] == 12 && board[56] == 10));
        return valid ? rights : -1;
    }

    private static int ParseEnPassant(string text, bool blackToMove, byte[] board)
    {
        if (text == "-")
        {
            return -1;
        }

        if (!Square.TryParse(text, out var square) || square.Rank != (blackToMove ? 2 : 5))
        {
            return -2;
        }

        // Precisa haver o peão adversário que acabou de avançar duas casas, com as casas de trás livres.
        var pawnSquare = square.Index + (blackToMove ? 8 : -8);
        var originSquare = square.Index + (blackToMove ? -8 : 8);
        var expectedPawn = blackToMove ? (byte)1 : (byte)7;
        return board[square.Index] == 0 && board[originSquare] == 0 && board[pawnSquare] == expectedPawn
            ? square.Index
            : -2;
    }

    // ---------- Geração de lances ----------

    private static Piece Decode(byte code) =>
        new(code >= 7 ? PieceColor.Black : PieceColor.White, (PieceType)((code - 1) % 6));

    private static bool IsBlack(byte code) => code >= 7;

    private static bool IsAttacked(ReadOnlySpan<byte> board, int square, bool byBlack)
    {
        var file = square & 7;
        var rank = square >> 3;
        var offset = byBlack ? 6 : 0;

        // Peões: um peão branco em (f±1, r-1) ataca; um preto em (f±1, r+1) ataca.
        var pawnRank = byBlack ? rank + 1 : rank - 1;
        if (pawnRank is >= 0 and <= 7)
        {
            var pawn = (byte)(1 + offset);
            if (file > 0 && board[(pawnRank * 8) + file - 1] == pawn)
            {
                return true;
            }

            if (file < 7 && board[(pawnRank * 8) + file + 1] == pawn)
            {
                return true;
            }
        }

        var knight = (byte)(2 + offset);
        var king = (byte)(6 + offset);
        for (var i = 0; i < 8; i++)
        {
            var f = file + KnightFiles[i];
            var r = rank + KnightRanks[i];
            if ((uint)f < 8 && (uint)r < 8 && board[(r * 8) + f] == knight)
            {
                return true;
            }

            f = file + KingFiles[i];
            r = rank + KingRanks[i];
            if ((uint)f < 8 && (uint)r < 8 && board[(r * 8) + f] == king)
            {
                return true;
            }
        }

        var bishop = (byte)(3 + offset);
        var rook = (byte)(4 + offset);
        var queen = (byte)(5 + offset);
        return SliderAttacks(board, file, rank, BishopFiles, BishopRanks, bishop, queen)
            || SliderAttacks(board, file, rank, RookFiles, RookRanks, rook, queen);
    }

    private static bool SliderAttacks(ReadOnlySpan<byte> board, int file, int rank, int[] dFiles, int[] dRanks, byte piece, byte queen)
    {
        for (var d = 0; d < dFiles.Length; d++)
        {
            var f = file + dFiles[d];
            var r = rank + dRanks[d];
            while ((uint)f < 8 && (uint)r < 8)
            {
                var found = board[(r * 8) + f];
                if (found != 0)
                {
                    if (found == piece || found == queen)
                    {
                        return true;
                    }

                    break;
                }

                f += dFiles[d];
                r += dRanks[d];
            }
        }

        return false;
    }

    private void GenerateLegal(List<Move> legal)
    {
        var pseudo = new List<Move>(64);
        GeneratePseudoLegal(pseudo);

        Span<byte> scratch = stackalloc byte[64];
        var ownKing = _blackToMove ? _blackKing : _whiteKing;
        foreach (var move in pseudo)
        {
            _board.CopyTo(scratch);
            MakeOnBoard(scratch, move, _enPassant);
            var kingSquare = _board[move.From.Index] is 6 or 12 ? move.To.Index : ownKing;
            if (!IsAttacked(scratch, kingSquare, !_blackToMove))
            {
                legal.Add(move);
            }
        }
    }

    private void GeneratePseudoLegal(List<Move> moves)
    {
        for (var from = 0; from < 64; from++)
        {
            var code = _board[from];
            if (code == 0 || IsBlack(code) != _blackToMove)
            {
                continue;
            }

            var file = from & 7;
            var rank = from >> 3;
            switch ((code - 1) % 6)
            {
                case 0:
                    GeneratePawn(moves, from, file, rank);
                    break;
                case 1:
                    GenerateSteps(moves, from, file, rank, KnightFiles, KnightRanks);
                    break;
                case 2:
                    GenerateSlides(moves, from, file, rank, BishopFiles, BishopRanks);
                    break;
                case 3:
                    GenerateSlides(moves, from, file, rank, RookFiles, RookRanks);
                    break;
                case 4:
                    GenerateSlides(moves, from, file, rank, BishopFiles, BishopRanks);
                    GenerateSlides(moves, from, file, rank, RookFiles, RookRanks);
                    break;
                default:
                    GenerateSteps(moves, from, file, rank, KingFiles, KingRanks);
                    GenerateCastling(moves, from);
                    break;
            }
        }
    }

    private void GeneratePawn(List<Move> moves, int from, int file, int rank)
    {
        var dir = _blackToMove ? -1 : 1;
        var startRank = _blackToMove ? 6 : 1;
        var lastRank = _blackToMove ? 0 : 7;

        var nextRank = rank + dir;
        if (_board[(nextRank * 8) + file] == 0)
        {
            AddPawnMove(moves, from, (nextRank * 8) + file, nextRank == lastRank);
            if (rank == startRank && _board[((rank + (2 * dir)) * 8) + file] == 0)
            {
                moves.Add(new Move(new Square(from), new Square(((rank + (2 * dir)) * 8) + file)));
            }
        }

        for (var df = -1; df <= 1; df += 2)
        {
            var f = file + df;
            if ((uint)f >= 8)
            {
                continue;
            }

            var to = (nextRank * 8) + f;
            var target = _board[to];
            if ((target != 0 && IsBlack(target) != _blackToMove) || (target == 0 && to == _enPassant))
            {
                AddPawnMove(moves, from, to, nextRank == lastRank);
            }
        }
    }

    private static void AddPawnMove(List<Move> moves, int from, int to, bool promotes)
    {
        if (!promotes)
        {
            moves.Add(new Move(new Square(from), new Square(to)));
            return;
        }

        foreach (var type in PromotionTypes)
        {
            moves.Add(new Move(new Square(from), new Square(to), type));
        }
    }

    private void GenerateSteps(List<Move> moves, int from, int file, int rank, int[] dFiles, int[] dRanks)
    {
        for (var i = 0; i < dFiles.Length; i++)
        {
            var f = file + dFiles[i];
            var r = rank + dRanks[i];
            if ((uint)f >= 8 || (uint)r >= 8)
            {
                continue;
            }

            var to = (r * 8) + f;
            var target = _board[to];
            if (target == 0 || IsBlack(target) != _blackToMove)
            {
                moves.Add(new Move(new Square(from), new Square(to)));
            }
        }
    }

    private void GenerateSlides(List<Move> moves, int from, int file, int rank, int[] dFiles, int[] dRanks)
    {
        for (var d = 0; d < dFiles.Length; d++)
        {
            var f = file + dFiles[d];
            var r = rank + dRanks[d];
            while ((uint)f < 8 && (uint)r < 8)
            {
                var to = (r * 8) + f;
                var target = _board[to];
                if (target == 0)
                {
                    moves.Add(new Move(new Square(from), new Square(to)));
                }
                else
                {
                    if (IsBlack(target) != _blackToMove)
                    {
                        moves.Add(new Move(new Square(from), new Square(to)));
                    }

                    break;
                }

                f += dFiles[d];
                r += dRanks[d];
            }
        }
    }

    private void GenerateCastling(List<Move> moves, int from)
    {
        var home = _blackToMove ? 60 : 4;
        if (from != home)
        {
            return;
        }

        var kingSide = _blackToMove ? BlackKingSide : WhiteKingSide;
        var queenSide = _blackToMove ? BlackQueenSide : WhiteQueenSide;
        var enemyIsBlack = !_blackToMove;
        var anyRight = (_castling & (kingSide | queenSide)) != 0;
        if (!anyRight || IsAttacked(_board, home, enemyIsBlack))
        {
            return;
        }

        if ((_castling & kingSide) != 0
            && _board[home + 1] == 0 && _board[home + 2] == 0
            && !IsAttacked(_board, home + 1, enemyIsBlack))
        {
            moves.Add(new Move(new Square(home), new Square(home + 2)));
        }

        if ((_castling & queenSide) != 0
            && _board[home - 1] == 0 && _board[home - 2] == 0 && _board[home - 3] == 0
            && !IsAttacked(_board, home - 1, enemyIsBlack))
        {
            moves.Add(new Move(new Square(home), new Square(home - 2)));
        }
    }

    // ---------- Aplicação ----------

    private static void MakeOnBoard(Span<byte> board, Move move, int enPassant)
    {
        var from = move.From.Index;
        var to = move.To.Index;
        var code = board[from];
        var isPawn = code is 1 or 7;

        if (isPawn && to == enPassant && board[to] == 0 && (from & 7) != (to & 7))
        {
            board[code == 1 ? to - 8 : to + 8] = 0;
        }

        if (code is 6 or 12 && Math.Abs(to - from) == 2)
        {
            var rookFrom = to > from ? from + 3 : from - 4;
            var rookTo = to > from ? from + 1 : from - 1;
            board[rookTo] = board[rookFrom];
            board[rookFrom] = 0;
        }

        board[to] = move.Promotion is { } promotion
            ? (byte)((int)promotion + 1 + (code >= 7 ? 6 : 0))
            : code;
        board[from] = 0;
    }

    private Position ApplyUnchecked(Move move)
    {
        var from = move.From.Index;
        var to = move.To.Index;
        var code = _board[from];
        var isPawn = code is 1 or 7;
        var isCapture = _board[to] != 0 || (isPawn && to == _enPassant && (from & 7) != (to & 7));

        var board = (byte[])_board.Clone();
        MakeOnBoard(board, move, _enPassant);

        var castling = _castling;
        if (code == 6)
        {
            castling &= ~(WhiteKingSide | WhiteQueenSide);
        }
        else if (code == 12)
        {
            castling &= ~(BlackKingSide | BlackQueenSide);
        }

        castling &= ~(RightFor(from) | RightFor(to));

        var enPassant = isPawn && Math.Abs(to - from) == 16 ? (from + to) / 2 : -1;
        var half = isPawn || isCapture ? 0 : HalfmoveClock + 1;
        var full = _blackToMove ? FullmoveNumber + 1 : FullmoveNumber;
        return new Position(board, !_blackToMove, castling, enPassant, half, full);
    }

    private static int RightFor(int square) => square switch
    {
        0 => WhiteQueenSide,
        7 => WhiteKingSide,
        56 => BlackQueenSide,
        63 => BlackKingSide,
        _ => 0,
    };
}
