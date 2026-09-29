using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessGameEndTests
{
    private static ChessGame FromFen(string fen) => new(Position.FromFen(fen));

    [Fact(DisplayName = "SPEC-0050:UT-04 — mate do pastor dá vitória das brancas por xeque-mate")]
    [Trait("Category", "SPEC-0050:UT-04")]
    public void ScholarsMate_ShouldBeWhiteCheckmate()
    {
        var game = new ChessGame();
        SanReplay.Line(game, "e2e4", "e7e5", "f1c4", "b8c6", "d1h5", "g8f6");
        Assert.Null(game.Result);

        var last = SanReplay.Mv(game, "h5", "f7");

        Assert.Equal("Qxf7#", last.San);
        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Checkmate), game.Result);
    }

    [Fact(DisplayName = "SPEC-0050:UT-04 — mate do louco dá vitória das pretas")]
    [Trait("Category", "SPEC-0050:UT-04")]
    public void FoolsMate_ShouldBeBlackCheckmate()
    {
        var game = new ChessGame();
        SanReplay.Line(game, "f2f3", "e7e5", "g2g4", "d8h4");

        Assert.Equal(new ChessResult(ChessOutcome.BlackWins, ChessEndReason.Checkmate), game.Result);
    }

    [Fact(DisplayName = "SPEC-0050:UT-04 — posição de afogamento é empate")]
    [Trait("Category", "SPEC-0050:UT-04")]
    public void Stalemate_ShouldBeDraw()
    {
        var game = FromFen("7k/8/5K2/8/8/8/8/6Q1 w - - 0 1");

        var last = SanReplay.Mv(game, "g1", "g6");

        Assert.False(last.IsCheck);
        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.Stalemate), game.Result);
    }

    [Theory(DisplayName = "SPEC-0050:UT-05 — material insuficiente encerra em empate automático")]
    [Trait("Category", "SPEC-0050:UT-05")]
    [InlineData("4k3/8/8/8/8/8/4n3/4K3 w - - 0 1", "e1", "e2")] // K x K
    [InlineData("4k3/8/8/8/8/8/4r3/3BK3 w - - 0 1", "d1", "e2")] // K+B x K
    [InlineData("4k3/8/8/8/8/8/4r3/3NK3 w - - 0 1", "e1", "e2")] // K+N x K
    [InlineData("4k3/8/8/5b2/8/8/4p3/4K2B w - - 0 1", "e1", "e2")] // bispos da mesma cor de casa
    [InlineData("4k3/8/8/8/8/5B2/4p3/4K2B w - - 0 1", "f3", "e2")] // vários bispos da mesma cor de casa
    public void InsufficientMaterial_ShouldDrawAutomatically(string fen, string from, string to)
    {
        var game = FromFen(fen);

        SanReplay.Mv(game, from, to);

        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.InsufficientMaterial), game.Result);
        Assert.False(game.TryPlay(Square.Parse("e8"), Square.Parse("d8"), null, out _));
    }

    [Theory(DisplayName = "SPEC-0050:UT-05 — material suficiente segue em jogo")]
    [Trait("Category", "SPEC-0050:UT-05")]
    [InlineData("4k1n1/8/8/8/8/8/4p3/3NK3 w - - 0 1")] // K+N x K+N
    [InlineData("4k3/8/8/8/8/8/4p3/2N1K1N1 w - - 0 1")] // K+N+N x K
    [InlineData("4k3/8/8/8/8/8/P3n3/4K3 w - - 0 1")] // K+P x K
    [InlineData("4k3/8/8/2b5/8/8/4p3/4K2B w - - 0 1")] // bispos de cores diferentes
    public void SufficientMaterial_ShouldKeepPlaying(string fen)
    {
        var game = FromFen(fen);

        SanReplay.Mv(game, "e1", "e2");

        Assert.Null(game.Result);
    }

    [Theory(DisplayName = "SPEC-0050:UT-05 — HasMatingMaterial é falso só para K, K+B e K+N")]
    [Trait("Category", "SPEC-0050:UT-05")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w - - 0 1", false)]
    [InlineData("4k3/8/8/8/8/8/8/3BK3 w - - 0 1", false)]
    [InlineData("4k3/8/8/8/8/8/8/3NK3 w - - 0 1", false)]
    [InlineData("4k3/8/8/8/8/8/8/2NNK3 w - - 0 1", true)]
    [InlineData("4k3/8/8/8/8/8/8/2BBK3 w - - 0 1", true)]
    [InlineData("4k3/8/8/8/8/8/8/2BNK3 w - - 0 1", true)]
    [InlineData("4k3/8/8/8/8/8/P7/4K3 w - - 0 1", true)]
    [InlineData("4k3/8/8/8/8/8/8/3RK3 w - - 0 1", true)]
    [InlineData("4k3/8/8/8/8/8/8/3QK3 w - - 0 1", true)]
    public void HasMatingMaterial_ShouldMatchRule(string fen, bool expectedForWhite)
    {
        var game = FromFen(fen);

        Assert.Equal(expectedForWhite, game.HasMatingMaterial(PieceColor.White));
        Assert.False(game.HasMatingMaterial(PieceColor.Black)); // só o rei preto
    }

    [Fact(DisplayName = "SPEC-0050:UT-06 — relógio de meio-lance em 99 e lance neutro empata por 50 lances")]
    [Trait("Category", "SPEC-0050:UT-06")]
    public void FiftyMoveRule_ShouldDrawAt100()
    {
        var game = FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 98 60");

        SanReplay.Mv(game, "a1", "a2");
        Assert.Null(game.Result);
        Assert.Equal(99, game.Position.HalfmoveClock);

        SanReplay.Mv(game, "e8", "d8");
        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.FiftyMoveRule), game.Result);
    }

    [Fact(DisplayName = "SPEC-0050:UT-06 — lance neutro em 99 chega a 100 e empata")]
    [Trait("Category", "SPEC-0050:UT-06")]
    public void FiftyMoveRule_NeutralMoveAt99_ShouldDraw()
    {
        var game = FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 99 60");

        SanReplay.Mv(game, "a1", "a2");

        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.FiftyMoveRule), game.Result);
    }

    [Theory(DisplayName = "SPEC-0050:UT-06 — captura ou lance de peão zera o relógio e evita o empate")]
    [Trait("Category", "SPEC-0050:UT-06")]
    [InlineData("4k3/8/8/8/8/8/4p3/R3K3 w - - 99 60", "e1", "e2")]
    [InlineData("4k3/8/8/8/8/8/P7/R3K3 w - - 99 60", "a2", "a3")]
    public void FiftyMoveRule_CaptureOrPawnMove_ShouldResetClock(string fen, string from, string to)
    {
        var game = FromFen(fen);

        SanReplay.Mv(game, from, to);

        Assert.Null(game.Result);
        Assert.Equal(0, game.Position.HalfmoveClock);
    }

    [Fact(DisplayName = "SPEC-0050:UT-06 — xeque-mate no lance 100 prevalece sobre a regra dos 50 lances")]
    [Trait("Category", "SPEC-0050:UT-06")]
    public void Checkmate_ShouldTakePrecedenceOverFiftyMoveRule()
    {
        var game = FromFen("6k1/5ppp/8/8/8/8/8/R3K3 w - - 99 60");

        var last = SanReplay.Mv(game, "a1", "a8");

        Assert.True(last.IsCheckmate);
        Assert.Equal(100, game.Position.HalfmoveClock);
        Assert.Equal(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Checkmate), game.Result);
    }

    [Fact(DisplayName = "SPEC-0050:UT-07 — terceira ocorrência da posição inicial encerra por repetição")]
    [Trait("Category", "SPEC-0050:UT-07")]
    public void Repetition_ThirdOccurrence_ShouldDraw()
    {
        var game = new ChessGame();

        SanReplay.Line(game, "g1f3", "g8f6", "f3g1", "f6g8");
        Assert.Null(game.Result);
        SanReplay.Line(game, "g1f3", "g8f6", "f3g1");
        Assert.Null(game.Result);
        SanReplay.Line(game, "f6g8");

        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.ThreefoldRepetition), game.Result);
    }

    [Fact(DisplayName = "SPEC-0050:UT-07 — direitos de roque diferentes não contam como repetição")]
    [Trait("Category", "SPEC-0050:UT-07")]
    public void Repetition_DifferentCastlingRights_ShouldNotCount()
    {
        var game = FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
        string[] cycle = ["a1b1", "a8b8", "b1a1", "b8a8"];

        SanReplay.Line(game, cycle);
        SanReplay.Line(game, cycle);
        Assert.Null(game.Result); // sem considerar o roque, a posição inicial já teria 3 ocorrências

        SanReplay.Line(game, cycle);
        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.ThreefoldRepetition), game.Result);
    }

    [Fact(DisplayName = "SPEC-0050:UT-07 — en passant sem captura legal não quebra a repetição")]
    [Trait("Category", "SPEC-0050:UT-07")]
    public void Repetition_EnPassantWithoutLegalCapture_ShouldNotBreakIt()
    {
        var game = new ChessGame();
        SanReplay.Line(game, "e2e4"); // alvo e3 sem peão preto ao lado
        string[] cycle = ["g8f6", "b1c3", "f6g8", "c3b1"];

        SanReplay.Line(game, cycle);
        Assert.Null(game.Result);
        SanReplay.Line(game, cycle);

        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.ThreefoldRepetition), game.Result);
    }

    [Fact(DisplayName = "SPEC-0050:UT-07 — en passant com captura legal entra na chave da posição")]
    [Trait("Category", "SPEC-0050:UT-07")]
    public void Repetition_EnPassantWithLegalCapture_ShouldDifferentiatePosition()
    {
        var game = FromFen("4k3/8/8/8/3p4/8/4P3/4K3 w - - 0 1");
        SanReplay.Line(game, "e2e4"); // d4 pode capturar en passant em e3
        string[] cycle = ["e8d8", "e1d1", "d8e8", "d1e1"];

        SanReplay.Line(game, cycle);
        SanReplay.Line(game, cycle);
        Assert.Null(game.Result); // sem o en passant na chave, já seriam 3 ocorrências

        SanReplay.Line(game, cycle);
        Assert.Equal(new ChessResult(ChessOutcome.Draw, ChessEndReason.ThreefoldRepetition), game.Result);
    }
}
