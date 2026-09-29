using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

/// <summary>
/// Auxiliar de teste (a leitura de SAN não é recurso do produto): acha o lance legal cujo SAN gerado
/// é igual ao texto, jogando cada candidato numa partida descartável.
/// </summary>
internal static class SanReplay
{
    public static ChessGame Play(string sanText, string? fen = null)
    {
        var game = new ChessGame(fen is null ? null : Position.FromFen(fen));
        foreach (var token in sanText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            Move? found = null;
            foreach (var candidate in game.LegalMoves())
            {
                var probe = new ChessGame(game.Position);
                if (probe.TryPlay(candidate.From, candidate.To, candidate.Promotion, out var played) && played!.San == token)
                {
                    found = candidate;
                    break;
                }
            }

            Assert.True(found is not null, $"Nenhum lance legal gera o SAN '{token}' em {game.Position.ToFen()}");
            Assert.True(game.TryPlay(found.Value.From, found.Value.To, found.Value.Promotion, out _));
        }

        return game;
    }

    public static ChessMove Mv(ChessGame game, string from, string to, PieceType? promotion = null)
    {
        Assert.True(game.TryPlay(Square.Parse(from), Square.Parse(to), promotion, out var played), $"{from}{to} deveria ser aceito");
        return played!;
    }

    public static void Line(ChessGame game, params string[] moves)
    {
        foreach (var move in moves)
        {
            Mv(game, move[..2], move[2..4], move.Length > 4 ? PromotionOf(move[4]) : null);
        }
    }

    private static PieceType PromotionOf(char letter) => letter switch
    {
        'q' => PieceType.Queen,
        'r' => PieceType.Rook,
        'b' => PieceType.Bishop,
        _ => PieceType.Knight,
    };
}

public class ChessSanTests
{
    private static string SanOf(string fen, string from, string to, PieceType? promotion = null) =>
        SanReplay.Mv(new ChessGame(Position.FromFen(fen)), from, to, promotion).San;

    [Fact(DisplayName = "SPEC-0050:UT-02 — SAN de peça, peão, captura de peão, xeque e mate")]
    [Trait("Category", "SPEC-0050:UT-02")]
    public void San_ShouldFollowContractForBasicMoves()
    {
        var game = new ChessGame();
        Assert.Equal("e4", SanReplay.Mv(game, "e2", "e4").San);
        Assert.Equal("d5", SanReplay.Mv(game, "d7", "d5").San);
        var capture = SanReplay.Mv(game, "e4", "d5");
        Assert.Equal("exd5", capture.San);
        Assert.Equal(PieceType.Pawn, capture.Piece);
        Assert.Equal(PieceType.Pawn, capture.Captured);
        Assert.False(capture.IsCheck);

        var knight = new ChessGame();
        Assert.Equal("Nf3", SanReplay.Mv(knight, "g1", "f3").San);

        var check = new ChessGame();
        SanReplay.Line(check, "e2e4", "f7f5");
        var queen = SanReplay.Mv(check, "d1", "h5");
        Assert.Equal("Qh5+", queen.San);
        Assert.True(queen.IsCheck);
        Assert.False(queen.IsCheckmate);

        var fools = new ChessGame();
        SanReplay.Line(fools, "f2f3", "e7e5", "g2g4");
        var mate = SanReplay.Mv(fools, "d8", "h4");
        Assert.Equal("Qh4#", mate.San);
        Assert.True(mate.IsCheck);
        Assert.True(mate.IsCheckmate);
    }

    [Fact(DisplayName = "SPEC-0050:UT-02 — SAN de roque curto e longo")]
    [Trait("Category", "SPEC-0050:UT-02")]
    public void San_ShouldWriteCastling()
    {
        const string Fen = "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1";
        Assert.Equal("O-O", SanOf(Fen, "e1", "g1"));
        Assert.Equal("O-O-O", SanOf(Fen, "e1", "c1"));
        const string BlackFen = "r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1";
        Assert.Equal("O-O", SanOf(BlackFen, "e8", "g8"));
        Assert.Equal("O-O-O", SanOf(BlackFen, "e8", "c8"));
    }

    [Fact(DisplayName = "SPEC-0050:UT-02 — SAN de promoção, com captura e xeque")]
    [Trait("Category", "SPEC-0050:UT-02")]
    public void San_ShouldWritePromotion()
    {
        Assert.Equal("exf8=Q+", SanOf("5r1k/4P3/8/8/8/8/8/K7 w - - 0 1", "e7", "f8", PieceType.Queen));
        Assert.Equal("a8=N", SanOf("8/P6k/8/8/8/8/8/K7 w - - 0 1", "a7", "a8", PieceType.Knight));
        Assert.Equal("a8=R", SanOf("8/P6k/8/8/8/8/8/K7 w - - 0 1", "a7", "a8", PieceType.Rook));
    }

    [Fact(DisplayName = "SPEC-0050:UT-02 — SAN de en passant usa a coluna de origem")]
    [Trait("Category", "SPEC-0050:UT-02")]
    public void San_ShouldWriteEnPassant() =>
        Assert.Equal("exd6", SanOf("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1", "e5", "d6"));

    [Fact(DisplayName = "SPEC-0050:UT-03 — desambiguação de cavalos por coluna")]
    [Trait("Category", "SPEC-0050:UT-03")]
    public void San_ShouldDisambiguateKnightsByFile()
    {
        const string Fen = "4k3/8/8/8/8/8/8/1N2KN2 w - - 0 1";
        Assert.Equal("Nbd2", SanOf(Fen, "b1", "d2"));
        Assert.Equal("Nfd2", SanOf(Fen, "f1", "d2"));
    }

    [Fact(DisplayName = "SPEC-0050:UT-03 — desambiguação de cavalos na mesma coluna por fileira")]
    [Trait("Category", "SPEC-0050:UT-03")]
    public void San_ShouldDisambiguateKnightsByRank()
    {
        const string Fen = "4k3/8/8/8/N7/8/N7/4K3 w - - 0 1";
        Assert.Equal("N4c3", SanOf(Fen, "a4", "c3"));
        Assert.Equal("N2c3", SanOf(Fen, "a2", "c3"));
    }

    [Fact(DisplayName = "SPEC-0050:UT-03 — desambiguação de torres")]
    [Trait("Category", "SPEC-0050:UT-03")]
    public void San_ShouldDisambiguateRooks()
    {
        const string Fen = "4k3/8/8/8/8/8/4K3/R6R w - - 0 1";
        Assert.Equal("Rad1", SanOf(Fen, "a1", "d1"));
        Assert.Equal("Rhd1", SanOf(Fen, "h1", "d1"));
    }

    [Fact(DisplayName = "SPEC-0050:UT-03 — três damas: coluna, fileira e casa completa")]
    [Trait("Category", "SPEC-0050:UT-03")]
    public void San_ShouldDisambiguateQueensByFileRankThenSquare()
    {
        const string Fen = "4k3/8/8/8/8/Q7/8/Q1Q4K w - - 0 1";
        Assert.Equal("Qa1c3", SanOf(Fen, "a1", "c3"));
        Assert.Equal("Q3c3", SanOf(Fen, "a3", "c3"));
        Assert.Equal("Qcc3", SanOf(Fen, "c1", "c3"));
    }

    [Fact(DisplayName = "SPEC-0050:UT-03 — peça cravada não conta na desambiguação")]
    [Trait("Category", "SPEC-0050:UT-03")]
    public void San_ShouldIgnorePinnedPieceWhenDisambiguating() =>
        Assert.Equal("Nc3", SanOf("4r1k1/8/8/8/8/8/4N3/1N2K3 w - - 0 1", "b1", "c3"));

    [Fact(DisplayName = "SPEC-0050:IT-01 — Ópera de Morphy reproduz SAN e mate")]
    [Trait("Category", "SPEC-0050:IT-01")]
    public void OperaGame_ShouldReplayToCheckmate()
    {
        const string Opera =
            "e4 e5 Nf3 d6 d4 Bg4 dxe5 Bxf3 Qxf3 dxe5 Bc4 Nf6 Qb3 Qe7 Nc3 c6 Bg5 b5 Nxb5 cxb5 Bxb5+ Nbd7 " +
            "O-O-O Rd8 Rxd7 Rxd7 Rd1 Qe6 Bxd7+ Nxd7 Qb8+ Nxb8 Rd8#";

        var game = SanReplay.Play(Opera);

        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Checkmate), game.Result);
        Assert.True(game.IsOver);
        Assert.Equal(Opera, game.MovesSan);
    }

    [Fact(DisplayName = "SPEC-0050:IT-01 — partida com roque, en passant e captura")]
    [Trait("Category", "SPEC-0050:IT-01")]
    public void CastlingAndEnPassantGame_ShouldReplay()
    {
        const string Line = "e4 Nf6 e5 d5 exd6 Qxd6 Nf3 Nc6 Bc4 e5 O-O Be6 Bxe6 fxe6";

        var game = SanReplay.Play(Line);

        Assert.Null(game.Result);
        Assert.Equal(Line, game.MovesSan);
        Assert.Contains(game.Moves, m => m.San == "O-O");
        Assert.Contains(game.Moves, m => m.San == "exd6" && m.Captured == PieceType.Pawn);
    }

    [Fact(DisplayName = "SPEC-0050:IT-01 — partida com promoção e captura")]
    [Trait("Category", "SPEC-0050:IT-01")]
    public void PromotionGame_ShouldReplay()
    {
        const string Line = "h4 g5 hxg5 h6 gxh6 Nf6 h7 Rg8 hxg8=Q Nxg8";

        var game = SanReplay.Play(Line);

        Assert.Null(game.Result);
        Assert.Equal(Line, game.MovesSan);
        Assert.Equal(PieceType.Rook, game.Moves[8].Captured);
        Assert.Equal(PieceType.Queen, game.Moves[8].Move.Promotion);
    }

    [Fact(DisplayName = "SPEC-0050:IT-01 — stalemate mais curto de Loyd termina em afogamento")]
    [Trait("Category", "SPEC-0050:IT-01")]
    public void LoydStalemate_ShouldEndInStalemate()
    {
        const string Line =
            "e3 a5 Qh5 Ra6 Qxa5 h5 h4 Rah6 Qxc7 f6 Qxd7+ Kf7 Qxb7 Qd3 Qxb8 Qh7 Qxc8 Kg6 Qe6";

        var game = SanReplay.Play(Line);

        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.Stalemate), game.Result);
        Assert.Equal(Line, game.MovesSan);
    }

    [Theory(DisplayName = "SPEC-0050:IT-02 — MovesSan reexecutado reconstrói a mesma posição final")]
    [Trait("Category", "SPEC-0050:IT-02")]
    [InlineData("e4 e5 Nf3 d6 d4 Bg4 dxe5 Bxf3 Qxf3 dxe5 Bc4 Nf6 Qb3 Qe7 Nc3 c6 Bg5 b5 Nxb5 cxb5 Bxb5+ Nbd7 O-O-O Rd8 Rxd7 Rxd7 Rd1 Qe6 Bxd7+ Nxd7 Qb8+ Nxb8 Rd8#")]
    [InlineData("e4 Nf6 e5 d5 exd6 Qxd6 Nf3 Nc6 Bc4 e5 O-O Be6 Bxe6 fxe6")]
    [InlineData("h4 g5 hxg5 h6 gxh6 Nf6 h7 Rg8 hxg8=Q Nxg8")]
    [InlineData("e3 a5 Qh5 Ra6 Qxa5 h5 h4 Rah6 Qxc7 f6 Qxd7+ Kf7 Qxb7 Qd3 Qxb8 Qh7 Qxc8 Kg6 Qe6")]
    public void MovesSan_ShouldRoundTripToSameFinalPosition(string line)
    {
        var original = SanReplay.Play(line);

        var replayed = SanReplay.Play(original.MovesSan);

        Assert.Equal(original.Position.ToFen(), replayed.Position.ToFen());
        Assert.Equal(original.MovesSan, replayed.MovesSan);
    }
}
