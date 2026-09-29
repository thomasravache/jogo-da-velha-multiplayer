using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

internal static class ChessTestHelpers
{
    public static string Uci(this Move move)
    {
        var promotion = move.Promotion switch
        {
            PieceType.Queen => "q",
            PieceType.Rook => "r",
            PieceType.Bishop => "b",
            PieceType.Knight => "n",
            null => string.Empty,
            _ => "?",
        };

        return move.From.ToString() + move.To.ToString() + promotion;
    }

    public static Move ParseMove(string uci)
    {
        var promotion = uci.Length == 5
            ? uci[4] switch
            {
                'q' => PieceType.Queen,
                'r' => PieceType.Rook,
                'b' => PieceType.Bishop,
                'n' => PieceType.Knight,
                _ => throw new ArgumentException("promoção inválida", nameof(uci)),
            }
            : (PieceType?)null;

        return new Move(Square.Parse(uci[..2]), Square.Parse(uci[2..4]), promotion);
    }

    public static List<string> MoveList(Position position, string? fromSquare = null) =>
        [.. position.LegalMoves()
            .Where(m => fromSquare is null || m.From.ToString() == fromSquare)
            .Select(m => m.Uci())
            .Order(StringComparer.Ordinal)];

    public static void AssertMoves(string fen, string? fromSquare, params string[] expected)
    {
        var actual = MoveList(Position.FromFen(fen), fromSquare);

        Assert.Equal([.. expected.Order(StringComparer.Ordinal)], actual);
    }

    public static bool HasMove(string fen, string uci) =>
        MoveList(Position.FromFen(fen)).Contains(uci);

    public static Position Play(string fen, string uci) =>
        Position.FromFen(fen).Apply(ParseMove(uci));
}

public class ChessMoveGenerationTests
{
    // ---------- UT-03: movimento das peças ----------

    [Fact(DisplayName = "SPEC-0049:UT-03 — cavalo livre tem 8 lances")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Knight_Free() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/3N4/8/8/4K3 w - - 0 1", "d4",
            "d4b3", "d4b5", "d4c2", "d4c6", "d4e2", "d4e6", "d4f3", "d4f5");

    [Fact(DisplayName = "SPEC-0049:UT-03 — cavalo bloqueado por peças aliadas não tem lances")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Knight_BlockedByOwnPieces() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/1P6/2P5/N3K3 w - - 0 1", "a1");

    [Fact(DisplayName = "SPEC-0049:UT-03 — cavalo captura peças adversárias")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Knight_Captures() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/1p6/2p5/N3K3 w - - 0 1", "a1", "a1b3", "a1c2");

    [Fact(DisplayName = "SPEC-0049:UT-03 — bispo livre tem 13 lances")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Bishop_Free() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/3B4/8/8/4K3 w - - 0 1", "d4",
            "d4a1", "d4b2", "d4c3", "d4e3", "d4f2", "d4g1", "d4c5", "d4b6", "d4a7", "d4e5", "d4f6", "d4g7", "d4h8");

    [Fact(DisplayName = "SPEC-0049:UT-03 — bispo bloqueado por aliada não anda")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Bishop_BlockedByOwnPiece() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/8/1P6/B3K3 w - - 0 1", "a1");

    [Fact(DisplayName = "SPEC-0049:UT-03 — bispo captura e não atravessa a peça capturada")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Bishop_CapturesAndStops() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/8/1p6/B3K3 w - - 0 1", "a1", "a1b2");

    [Fact(DisplayName = "SPEC-0049:UT-03 — torre livre tem 14 lances")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Rook_Free() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/3R4/8/8/4K3 w - - 0 1", "d4",
            "d4d1", "d4d2", "d4d3", "d4d5", "d4d6", "d4d7", "d4d8",
            "d4a4", "d4b4", "d4c4", "d4e4", "d4f4", "d4g4", "d4h4");

    [Fact(DisplayName = "SPEC-0049:UT-03 — torre bloqueada e com captura")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Rook_BlockedAndCapture() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/8/P7/R3K3 w - - 0 1", "a1", "a1b1", "a1c1", "a1d1");

    [Fact(DisplayName = "SPEC-0049:UT-03 — dama livre tem 27 lances")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Queen_Free()
    {
        var moves = ChessTestHelpers.MoveList(Position.FromFen("4k3/8/8/8/3Q4/8/8/4K3 w - - 0 1"), "d4");

        Assert.Equal(27, moves.Count);
        Assert.Contains("d4h8", moves);
        Assert.Contains("d4a4", moves);
        Assert.Contains("d4d8", moves);
        Assert.Contains("d4g1", moves);
    }

    [Fact(DisplayName = "SPEC-0049:UT-03 — dama com bloqueio e captura")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Queen_BlockedAndCapture() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/8/1p1P4/Q3K3 w - - 0 1", "a1", "a1a2", "a1a3", "a1a4", "a1a5", "a1a6", "a1a7", "a1a8", "a1b1", "a1c1", "a1d1", "a1b2");

    [Fact(DisplayName = "SPEC-0049:UT-03 — rei no centro tem 8 lances")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void King_Free() =>
        ChessTestHelpers.AssertMoves("7k/8/8/8/4K3/8/8/8 w - - 0 1", "e4",
            "e4d3", "e4d4", "e4d5", "e4e3", "e4e5", "e4f3", "e4f4", "e4f5");

    [Fact(DisplayName = "SPEC-0049:UT-03 — rei cercado por aliadas não anda")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void King_BlockedByOwnPieces() =>
        ChessTestHelpers.AssertMoves("7k/8/8/8/8/8/PP6/KR6 w - - 0 1", "a1");

    [Fact(DisplayName = "SPEC-0049:UT-03 — rei não entra em casa atacada")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void King_CannotEnterAttackedSquare() =>
        ChessTestHelpers.AssertMoves("7k/8/8/8/8/8/3r4/K7 w - - 0 1", "a1", "a1b1");

    [Fact(DisplayName = "SPEC-0049:UT-03 — peão na casa inicial avança uma ou duas casas")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Pawn_InitialSquare() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1", "e2", "e2e3", "e2e4");

    [Fact(DisplayName = "SPEC-0049:UT-03 — peão captura na diagonal e avança")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Pawn_Captures() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/3p1p2/4P3/4K3 w - - 0 1", "e2", "e2e3", "e2e4", "e2d3", "e2f3");

    [Fact(DisplayName = "SPEC-0049:UT-03 — peão bloqueado de frente não avança")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Pawn_BlockedInFront() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/4n3/4P3/4K3 w - - 0 1", "e2");

    [Fact(DisplayName = "SPEC-0049:UT-03 — peão não salta peça no avanço duplo")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Pawn_DoublePushBlockedOnSecondSquare() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/4n3/8/4P3/4K3 w - - 0 1", "e2", "e2e3");

    [Fact(DisplayName = "SPEC-0049:UT-03 — peão fora da casa inicial avança uma casa")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Pawn_NotOnInitialSquare() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/4P3/8/4K3 w - - 0 1", "e3", "e3e4");

    [Fact(DisplayName = "SPEC-0049:UT-03 — peão preto anda para baixo")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void Pawn_Black() =>
        ChessTestHelpers.AssertMoves("4k3/4p3/8/8/8/8/8/4K3 b - - 0 1", "e7", "e7e6", "e7e5");

    [Fact(DisplayName = "SPEC-0049:UT-03 — posição inicial tem 20 lances")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void StartPosition_Has20Moves() => Assert.Equal(20, Position.Start.LegalMoves().Count);

    // ---------- UT-04: roque ----------

    private const string CastleFree = "r3k2r/8/8/8/8/8/8/R3K2R";

    [Theory(DisplayName = "SPEC-0049:UT-04 — roque livre, nos dois lados e nas duas cores")]
    [Trait("Category", "SPEC-0049:UT-04")]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1g1", true)]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1c1", true)]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1", "e8g8", true)]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1", "e8c8", true)]
    [InlineData("r3k2r/8/8/8/8/8/8/R2QK1NR w KQkq - 0 1", "e1g1", false)]
    [InlineData("r3k2r/8/8/8/8/8/8/R2QK1NR w KQkq - 0 1", "e1c1", false)]
    [InlineData("r3k2r/8/8/8/8/8/8/RN2K2R w KQkq - 0 1", "e1c1", false)]
    [InlineData("r3k2r/8/8/8/8/8/8/RN2K2R w KQkq - 0 1", "e1g1", true)]
    [InlineData("r2qk1nr/8/8/8/8/8/8/R3K2R b KQkq - 0 1", "e8c8", false)]
    [InlineData("r2qk1nr/8/8/8/8/8/8/R3K2R b KQkq - 0 1", "e8g8", false)]
    [InlineData("5rk1/8/8/8/8/8/8/R3K2R w KQ - 0 1", "e1g1", false)]
    [InlineData("5rk1/8/8/8/8/8/8/R3K2R w KQ - 0 1", "e1c1", true)]
    [InlineData("3r2k1/8/8/8/8/8/8/R3K2R w KQ - 0 1", "e1c1", false)]
    [InlineData("3r2k1/8/8/8/8/8/8/R3K2R w KQ - 0 1", "e1g1", true)]
    [InlineData("1r4k1/8/8/8/8/8/8/R3K2R w KQ - 0 1", "e1c1", true)]
    [InlineData("4r1k1/8/8/8/8/8/8/R3K2R w KQ - 0 1", "e1g1", false)]
    [InlineData("4r1k1/8/8/8/8/8/8/R3K2R w KQ - 0 1", "e1c1", false)]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R w Qkq - 0 1", "e1g1", false)]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R w Qkq - 0 1", "e1c1", true)]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R w - - 0 1", "e1c1", false)]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R b Kk - 0 1", "e8c8", false)]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R b Kk - 0 1", "e8g8", true)]
    public void Castling_OnlyWhenAllowed(string fen, string uci, bool expected) =>
        Assert.Equal(expected, ChessTestHelpers.HasMove(fen, uci));

    [Fact(DisplayName = "SPEC-0049:UT-04 — Apply do roque move o rei e a torre (brancas)")]
    [Trait("Category", "SPEC-0049:UT-04")]
    public void Castling_Apply_White()
    {
        var kingSide = ChessTestHelpers.Play($"{CastleFree} w KQkq - 0 1", "e1g1");
        var queenSide = ChessTestHelpers.Play($"{CastleFree} w KQkq - 0 1", "e1c1");

        Assert.Equal("r3k2r/8/8/8/8/8/8/R4RK1 b kq - 1 1", kingSide.ToFen());
        Assert.Equal("r3k2r/8/8/8/8/8/8/2KR3R b kq - 1 1", queenSide.ToFen());
        Assert.Equal(new Piece(PieceColor.White, PieceType.Rook), kingSide.PieceAt(Square.Parse("f1")));
        Assert.Null(kingSide.PieceAt(Square.Parse("h1")));
        Assert.Equal(new Piece(PieceColor.White, PieceType.Rook), queenSide.PieceAt(Square.Parse("d1")));
        Assert.Null(queenSide.PieceAt(Square.Parse("a1")));
    }

    [Fact(DisplayName = "SPEC-0049:UT-04 — Apply do roque move o rei e a torre (pretas)")]
    [Trait("Category", "SPEC-0049:UT-04")]
    public void Castling_Apply_Black()
    {
        var kingSide = ChessTestHelpers.Play($"{CastleFree} b KQkq - 4 9", "e8g8");
        var queenSide = ChessTestHelpers.Play($"{CastleFree} b KQkq - 4 9", "e8c8");

        Assert.Equal("r4rk1/8/8/8/8/8/8/R3K2R w KQ - 5 10", kingSide.ToFen());
        Assert.Equal("2kr3r/8/8/8/8/8/8/R3K2R w KQ - 5 10", queenSide.ToFen());
    }

    [Fact(DisplayName = "SPEC-0049:UT-04 — mover uma torre perde só o direito daquele lado")]
    [Trait("Category", "SPEC-0049:UT-04")]
    public void Castling_RookMoveLosesOneRight() =>
        Assert.Equal("r3k2r/8/8/8/8/8/R7/4K2R b Kkq - 1 1", ChessTestHelpers.Play($"{CastleFree} w KQkq - 0 1", "a1a2").ToFen());

    [Fact(DisplayName = "SPEC-0049:UT-04 — mover o rei perde os dois direitos")]
    [Trait("Category", "SPEC-0049:UT-04")]
    public void Castling_KingMoveLosesBothRights() =>
        Assert.Equal("r3k2r/8/8/8/8/8/4K3/R6R b kq - 1 1", ChessTestHelpers.Play($"{CastleFree} w KQkq - 0 1", "e1e2").ToFen());

    [Fact(DisplayName = "SPEC-0049:UT-04 — torre capturada perde o direito de roque do dono")]
    [Trait("Category", "SPEC-0049:UT-04")]
    public void Castling_RookCapturedLosesRight()
    {
        var after = ChessTestHelpers.Play("r3k2r/8/8/8/8/8/6b1/R3K2R b KQkq - 0 1", "g2h1");

        Assert.False(after.CanCastle(PieceColor.White, kingSide: true));
        Assert.True(after.CanCastle(PieceColor.White, kingSide: false));
        Assert.True(after.CanCastle(PieceColor.Black, kingSide: true));
        Assert.True(after.CanCastle(PieceColor.Black, kingSide: false));
        Assert.DoesNotContain("e1g1", ChessTestHelpers.MoveList(after));
    }

    // ---------- UT-05: en passant ----------

    [Fact(DisplayName = "SPEC-0049:UT-05 — en passant é gerado quando disponível")]
    [Trait("Category", "SPEC-0049:UT-05")]
    public void EnPassant_Available() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 2", "e5", "e5e6", "e5d6");

    [Fact(DisplayName = "SPEC-0049:UT-05 — sem alvo de en passant o lance não existe")]
    [Trait("Category", "SPEC-0049:UT-05")]
    public void EnPassant_NotAvailableWithoutTarget() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/3pP3/8/8/8/4K3 w - - 0 2", "e5", "e5e6");

    [Fact(DisplayName = "SPEC-0049:UT-05 — en passant que exporia o rei na fileira não é gerado")]
    [Trait("Category", "SPEC-0049:UT-05")]
    public void EnPassant_DiscoveredCheckIsIllegal() =>
        ChessTestHelpers.AssertMoves("8/8/8/K2pP2r/8/8/8/7k w - d6 0 1", "e5", "e5e6");

    [Fact(DisplayName = "SPEC-0049:UT-05 — en passant das pretas")]
    [Trait("Category", "SPEC-0049:UT-05")]
    public void EnPassant_Black() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/3pP3/8/8/4K3 b - e3 0 1", "d4", "d4d3", "d4e3");

    [Fact(DisplayName = "SPEC-0049:UT-05 — en passant que resolve um xeque de peão é gerado")]
    [Trait("Category", "SPEC-0049:UT-05")]
    public void EnPassant_CanCapturePawnGivingCheck() =>
        ChessTestHelpers.AssertMoves("8/8/8/2k5/3Pp3/8/8/4K3 b - d3 0 1", "e4", "e4d3");

    // ---------- UT-06: promoção ----------

    [Fact(DisplayName = "SPEC-0049:UT-06 — promoção sem captura gera 4 lances")]
    [Trait("Category", "SPEC-0049:UT-06")]
    public void Promotion_Push() =>
        ChessTestHelpers.AssertMoves("7k/4P3/8/8/8/8/8/4K3 w - - 0 1", "e7", "e7e8q", "e7e8r", "e7e8b", "e7e8n");

    [Fact(DisplayName = "SPEC-0049:UT-06 — promoção com captura gera 4 lances por destino")]
    [Trait("Category", "SPEC-0049:UT-06")]
    public void Promotion_PushAndCapture() =>
        ChessTestHelpers.AssertMoves("3r2k1/4P3/8/8/8/8/8/4K3 w - - 0 1", "e7",
            "e7e8q", "e7e8r", "e7e8b", "e7e8n", "e7d8q", "e7d8r", "e7d8b", "e7d8n");

    [Fact(DisplayName = "SPEC-0049:UT-06 — promoção das pretas")]
    [Trait("Category", "SPEC-0049:UT-06")]
    public void Promotion_Black() =>
        ChessTestHelpers.AssertMoves("7k/8/8/8/8/8/p7/4K3 b - - 0 1", "a2", "a2a1q", "a2a1r", "a2a1b", "a2a1n");

    [Fact(DisplayName = "SPEC-0049:UT-06 — promoção não gera lance sem peça escolhida")]
    [Trait("Category", "SPEC-0049:UT-06")]
    public void Promotion_RequiresPiece()
    {
        var position = Position.FromFen("7k/4P3/8/8/8/8/8/4K3 w - - 0 1");

        Assert.False(position.IsLegal(new Move(Square.Parse("e7"), Square.Parse("e8"))));
        Assert.False(position.IsLegal(new Move(Square.Parse("e7"), Square.Parse("e8"), PieceType.King)));
        Assert.False(position.IsLegal(new Move(Square.Parse("e7"), Square.Parse("e8"), PieceType.Pawn)));
        Assert.True(position.IsLegal(new Move(Square.Parse("e7"), Square.Parse("e8"), PieceType.Queen)));
    }

    // ---------- UT-07: xeque e cravadas ----------

    [Fact(DisplayName = "SPEC-0049:UT-07 — xeque simples: mover o rei ou bloquear")]
    [Trait("Category", "SPEC-0049:UT-07")]
    public void Check_Single() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/3B4/8/4K2r w - - 0 1", null, "e1d2", "e1e2", "e1f2", "d3f1");

    [Fact(DisplayName = "SPEC-0049:UT-07 — xeque simples: capturar o atacante")]
    [Trait("Category", "SPEC-0049:UT-07")]
    public void Check_CaptureAttacker() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/7R/8/8/4K2r w - - 0 1", null, "e1d2", "e1e2", "e1f2", "h4h1");

    [Fact(DisplayName = "SPEC-0049:UT-07 — xeque duplo: só o rei anda")]
    [Trait("Category", "SPEC-0049:UT-07")]
    public void Check_Double() =>
        ChessTestHelpers.AssertMoves("4k3/8/8/8/8/5n2/3B4/4K2r w - - 0 1", null, "e1e2", "e1f2");

    [Fact(DisplayName = "SPEC-0049:UT-07 — peça cravada só anda na linha da cravada")]
    [Trait("Category", "SPEC-0049:UT-07")]
    public void Pin_RookMovesAlongPin() =>
        ChessTestHelpers.AssertMoves("4q1k1/8/8/8/8/8/4R3/4K3 w - - 0 1", "e2", "e2e3", "e2e4", "e2e5", "e2e6", "e2e7", "e2e8");

    [Fact(DisplayName = "SPEC-0049:UT-07 — cavalo cravado não tem lances")]
    [Trait("Category", "SPEC-0049:UT-07")]
    public void Pin_KnightCannotMove() =>
        ChessTestHelpers.AssertMoves("4q1k1/8/8/8/8/8/4N3/4K3 w - - 0 1", "e2");

    [Fact(DisplayName = "SPEC-0049:UT-07 — bispo cravado na diagonal pode capturar o cravador")]
    [Trait("Category", "SPEC-0049:UT-07")]
    public void Pin_BishopMovesAlongDiagonal() =>
        ChessTestHelpers.AssertMoves("6k1/8/8/8/7b/8/5B2/4K3 w - - 0 1", "f2", "f2g3", "f2h4");

    [Fact(DisplayName = "SPEC-0049:UT-07 — peão cravado só avança na linha")]
    [Trait("Category", "SPEC-0049:UT-07")]
    public void Pin_PawnAlongFileOnly() =>
        ChessTestHelpers.AssertMoves("4r1k1/8/8/8/8/8/4P3/4K3 w - - 0 1", "e2", "e2e3", "e2e4");

    [Fact(DisplayName = "SPEC-0049:UT-07 — peão cravado na diagonal não avança")]
    [Trait("Category", "SPEC-0049:UT-07")]
    public void Pin_PawnDiagonalCannotAdvance() =>
        ChessTestHelpers.AssertMoves("6k1/8/8/8/7b/8/5P2/4K3 w - - 0 1", "f2");

    // ---------- UT-08: Apply ----------

    [Fact(DisplayName = "SPEC-0049:UT-08 — lance comum atualiza lado, relógio e mantém roque")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void Apply_Ordinary() =>
        Assert.Equal("rnbqkbnr/pppppppp/8/8/8/5N2/PPPPPPPP/RNBQKB1R b KQkq - 1 1", Position.Start.Apply(ChessTestHelpers.ParseMove("g1f3")).ToFen());

    [Fact(DisplayName = "SPEC-0049:UT-08 — avanço duplo define o alvo de en passant e o lance das pretas incrementa o número")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void Apply_DoublePush()
    {
        var afterWhite = Position.Start.Apply(ChessTestHelpers.ParseMove("e2e4"));
        var afterBlack = afterWhite.Apply(ChessTestHelpers.ParseMove("e7e5"));

        Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", afterWhite.ToFen());
        Assert.Equal(Square.Parse("e3"), afterWhite.EnPassantTarget);
        Assert.Equal("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2", afterBlack.ToFen());
        Assert.Equal(2, afterBlack.FullmoveNumber);
    }

    [Fact(DisplayName = "SPEC-0049:UT-08 — o alvo de en passant some no lance seguinte")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void Apply_EnPassantTargetIsCleared() =>
        Assert.Null(Position.Start.Apply(ChessTestHelpers.ParseMove("e2e4")).Apply(ChessTestHelpers.ParseMove("g8f6")).EnPassantTarget);

    [Fact(DisplayName = "SPEC-0049:UT-08 — captura zera o relógio de meio-lance; lance sem captura o incrementa")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void Apply_HalfmoveClock()
    {
        Assert.Equal("4k3/8/8/3N4/8/8/8/4K3 b - - 0 20", ChessTestHelpers.Play("4k3/8/8/3p4/8/2N5/8/4K3 w - - 7 20", "c3d5").ToFen());
        Assert.Equal("4k3/8/8/3p4/8/8/8/1N2K3 b - - 8 20", ChessTestHelpers.Play("4k3/8/8/3p4/8/2N5/8/4K3 w - - 7 20", "c3b1").ToFen());
        Assert.Equal("4k3/8/8/3p4/4P3/8/8/4K3 b - e3 0 20", ChessTestHelpers.Play("4k3/8/8/3p4/8/8/4P3/4K3 w - - 7 20", "e2e4").ToFen());
    }

    [Fact(DisplayName = "SPEC-0049:UT-08 — en passant remove o peão capturado")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void Apply_EnPassant()
    {
        Assert.Equal("4k3/8/3P4/8/8/8/8/4K3 b - - 0 2", ChessTestHelpers.Play("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 2", "e5d6").ToFen());
        Assert.Equal("4k3/8/8/8/8/4p3/8/4K3 w - - 0 2", ChessTestHelpers.Play("4k3/8/8/8/3pP3/8/8/4K3 b - e3 0 1", "d4e3").ToFen());
    }

    [Fact(DisplayName = "SPEC-0049:UT-08 — promoção troca o peão pela peça escolhida")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void Apply_Promotion()
    {
        Assert.Equal("4Q2k/8/8/8/8/8/8/4K3 b - - 0 5", ChessTestHelpers.Play("7k/4P3/8/8/8/8/8/4K3 w - - 3 5", "e7e8q").ToFen());
        Assert.Equal("4N2k/8/8/8/8/8/8/4K3 b - - 0 5", ChessTestHelpers.Play("7k/4P3/8/8/8/8/8/4K3 w - - 3 5", "e7e8n").ToFen());
        Assert.Equal("7k/8/8/8/8/8/8/q3K3 w - - 0 2", ChessTestHelpers.Play("7k/8/8/8/8/8/p7/4K3 b - - 0 1", "a2a1q").ToFen());
    }

    [Fact(DisplayName = "SPEC-0049:UT-08 — captura de torre na casa inicial por promoção remove o direito de roque")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void Apply_PromotionCaptureOfRookRemovesRight()
    {
        var after = ChessTestHelpers.Play("4k2r/6P1/8/8/8/8/8/4K3 w k - 0 1", "g7h8q");

        Assert.False(after.CanCastle(PieceColor.Black, kingSide: true));
    }

    [Fact(DisplayName = "SPEC-0049:UT-08 — Apply não altera a posição original (imutável)")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void Apply_DoesNotMutateOriginal()
    {
        var start = Position.Start;

        _ = start.Apply(ChessTestHelpers.ParseMove("e2e4"));

        Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", start.ToFen());
    }

    [Theory(DisplayName = "SPEC-0049:UT-08 — lance ilegal lança ArgumentException")]
    [Trait("Category", "SPEC-0049:UT-08")]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "e2e5")]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "e7e5")]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "e4e5")]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", "e1g1")]
    [InlineData("4k3/8/8/8/8/8/3r4/4K3 w - - 0 1", "e1d1")]
    [InlineData("4k3/8/8/8/8/8/8/4K2r w - - 0 1", "e1f1")]
    public void Apply_IllegalMove_ShouldThrow(string fen, string uci)
    {
        var position = Position.FromFen(fen);
        var move = ChessTestHelpers.ParseMove(uci);

        Assert.False(position.IsLegal(move));
        Assert.Throws<ArgumentException>(() => position.Apply(move));
    }

    [Fact(DisplayName = "SPEC-0049:UT-08 — IsLegal reconhece lances legais")]
    [Trait("Category", "SPEC-0049:UT-08")]
    public void IsLegal_AcceptsLegalMoves() =>
        Assert.True(Position.Start.IsLegal(ChessTestHelpers.ParseMove("e2e4")));

    // ---------- UT-09: IsInCheck ----------

    [Theory(DisplayName = "SPEC-0049:UT-09 — IsInCheck por tipo de peça")]
    [Trait("Category", "SPEC-0049:UT-09")]
    [InlineData("4k3/8/8/8/8/8/3p4/4K3 w - - 0 1", PieceColor.White, true)]
    [InlineData("4k3/8/8/8/8/8/3p4/4K3 w - - 0 1", PieceColor.Black, false)]
    [InlineData("4k3/8/8/8/8/5n2/8/4K3 w - - 0 1", PieceColor.White, true)]
    [InlineData("4k3/8/8/8/1b6/8/8/4K3 w - - 0 1", PieceColor.White, true)]
    [InlineData("4k3/8/8/8/8/8/8/r3K3 w - - 0 1", PieceColor.White, true)]
    [InlineData("4k3/8/8/8/8/8/8/4K2q w - - 0 1", PieceColor.White, true)]
    [InlineData("4k3/8/8/8/7q/8/8/4K3 w - - 0 1", PieceColor.White, true)]
    [InlineData("4k3/8/8/8/8/8/8/r1N1K3 w - - 0 1", PieceColor.White, false)]
    [InlineData("4k3/8/8/8/8/8/3P4/4K3 w - - 0 1", PieceColor.White, false)]
    [InlineData("4k3/8/8/8/8/2p5/8/4K3 w - - 0 1", PieceColor.White, false)]
    [InlineData("4k3/8/8/8/8/8/8/4R1K1 b - - 0 1", PieceColor.Black, true)]
    [InlineData("4k3/8/8/8/8/8/8/4R1K1 b - - 0 1", PieceColor.White, false)]
    [InlineData("4k3/3P4/8/8/8/8/8/6K1 b - - 0 1", PieceColor.Black, true)]
    [InlineData("4k3/8/5N2/8/8/8/8/6K1 b - - 0 1", PieceColor.Black, true)]
    [InlineData("4k3/8/8/8/B7/8/8/6K1 b - - 0 1", PieceColor.Black, true)]
    [InlineData("4k3/8/8/8/8/8/8/4Q1K1 b - - 0 1", PieceColor.Black, true)]
    [InlineData("4k3/8/8/8/8/8/8/6K1 b - - 0 1", PieceColor.Black, false)]
    public void IsInCheck_ShouldBeCorrect(string fen, PieceColor color, bool expected) =>
        Assert.Equal(expected, Position.FromFen(fen).IsInCheck(color));

    [Fact(DisplayName = "SPEC-0049:UT-09 — a posição inicial não tem xeque")]
    [Trait("Category", "SPEC-0049:UT-09")]
    public void IsInCheck_StartPosition()
    {
        Assert.False(Position.Start.IsInCheck(PieceColor.White));
        Assert.False(Position.Start.IsInCheck(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0049:UT-03 — A lista de lances legais devolvida não permite corromper o cache da posição")]
    [Trait("Category", "SPEC-0049:UT-03")]
    public void LegalMoves_ShouldNotExposeMutableCache()
    {
        var position = Position.Start;

        Assert.False(position.LegalMoves() is System.Collections.Generic.List<Move>);
        Assert.Equal(20, position.LegalMoves().Count);
    }
}
