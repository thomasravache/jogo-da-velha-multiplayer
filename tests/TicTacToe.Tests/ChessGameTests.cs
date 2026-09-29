using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessGameTests
{
    [Fact(DisplayName = "SPEC-0050:UT-01 — lance legal avança o estado e registra o histórico")]
    [Trait("Category", "SPEC-0050:UT-01")]
    public void TryPlay_LegalMove_ShouldAdvanceState()
    {
        var game = new ChessGame();

        var ok = game.TryPlay(Square.Parse("e2"), Square.Parse("e4"), null, out var played);

        Assert.True(ok);
        Assert.NotNull(played);
        Assert.Equal("e4", played.San);
        Assert.Single(game.Moves);
        Assert.Equal(PieceColor.Black, game.Position.SideToMove);
        Assert.Null(game.Result);
        Assert.False(game.IsOver);
    }

    [Theory(DisplayName = "SPEC-0050:UT-01 — lance ilegal, casa vazia ou fora da vez é recusado sem alterar o estado")]
    [Trait("Category", "SPEC-0050:UT-01")]
    [InlineData("e2", "e5")]
    [InlineData("e4", "e5")]
    [InlineData("e7", "e5")]
    [InlineData("a1", "a3")]
    public void TryPlay_InvalidMove_ShouldRefuseAndKeepState(string from, string to)
    {
        var game = new ChessGame();
        var before = game.Position;

        var ok = game.TryPlay(Square.Parse(from), Square.Parse(to), null, out var played);

        Assert.False(ok);
        Assert.Null(played);
        Assert.Same(before, game.Position);
        Assert.Empty(game.Moves);
    }

    [Theory(DisplayName = "SPEC-0050:UT-01 — promoção ausente ou inválida é recusada")]
    [Trait("Category", "SPEC-0050:UT-01")]
    [InlineData(null)]
    [InlineData(PieceType.King)]
    [InlineData(PieceType.Pawn)]
    public void TryPlay_PromotionMissingOrInvalid_ShouldRefuse(PieceType? promotion)
    {
        var game = new ChessGame(Position.FromFen("8/P6k/8/8/8/8/8/K7 w - - 0 1"));

        var ok = game.TryPlay(Square.Parse("a7"), Square.Parse("a8"), promotion, out var played);

        Assert.False(ok);
        Assert.Null(played);
        Assert.Empty(game.Moves);
    }

    [Fact(DisplayName = "SPEC-0050:UT-01 — promoção válida é aceita e NeedsPromotion detecta o caso")]
    [Trait("Category", "SPEC-0050:UT-01")]
    public void TryPlay_ValidPromotion_ShouldSucceed()
    {
        var game = new ChessGame(Position.FromFen("8/P6k/8/8/8/8/8/K7 w - - 0 1"));

        Assert.True(game.NeedsPromotion(Square.Parse("a7"), Square.Parse("a8")));
        Assert.False(game.NeedsPromotion(Square.Parse("a1"), Square.Parse("a2")));
        Assert.True(game.TryPlay(Square.Parse("a7"), Square.Parse("a8"), PieceType.Queen, out var played));
        Assert.Equal("a8=Q", played!.San);
        Assert.Equal(PieceType.Queen, game.Position.PieceAt(Square.Parse("a8"))!.Value.Type);
    }

    [Fact(DisplayName = "SPEC-0050:UT-01 — LegalMoves e DestinationsFrom refletem a posição")]
    [Trait("Category", "SPEC-0050:UT-01")]
    public void LegalMoves_AndDestinations_ShouldReflectPosition()
    {
        var game = new ChessGame();

        Assert.Equal(20, game.LegalMoves().Count);
        Assert.Equal(
            ["e3", "e4"],
            game.DestinationsFrom(Square.Parse("e2")).Select(s => s.ToString()).Order(StringComparer.Ordinal).ToArray());
        Assert.Empty(game.DestinationsFrom(Square.Parse("e5")));

        var promo = new ChessGame(Position.FromFen("8/P6k/8/8/8/8/8/K7 w - - 0 1"));
        Assert.Single(promo.DestinationsFrom(Square.Parse("a7")));
    }

    [Fact(DisplayName = "SPEC-0050:UT-08 — CapturedBy lista as peças capturadas por cor")]
    [Trait("Category", "SPEC-0050:UT-08")]
    public void CapturedBy_ShouldListCapturedPiecesPerColor()
    {
        var game = new ChessGame();
        SanReplay.Line(game, "e2e4", "d7d5", "e4d5", "d8d5", "b1c3", "d5g2");
        Assert.Equal([PieceType.Pawn], game.CapturedBy(PieceColor.White));
        Assert.Equal([PieceType.Pawn, PieceType.Pawn], game.CapturedBy(PieceColor.Black));

        SanReplay.Line(game, "f1g2");
        Assert.Equal([PieceType.Pawn, PieceType.Queen], game.CapturedBy(PieceColor.White));
    }

    [Fact(DisplayName = "SPEC-0050:UT-08 — en passant conta o peão capturado")]
    [Trait("Category", "SPEC-0050:UT-08")]
    public void CapturedBy_EnPassant_ShouldCountPawn()
    {
        var game = new ChessGame(Position.FromFen("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1"));

        var played = SanReplay.Mv(game, "e5", "d6");

        Assert.Equal(PieceType.Pawn, played.Captured);
        Assert.Equal([PieceType.Pawn], game.CapturedBy(PieceColor.White));
        Assert.Empty(game.CapturedBy(PieceColor.Black));
    }

    [Fact(DisplayName = "SPEC-0050:UT-09 — End só encerra partida em andamento")]
    [Trait("Category", "SPEC-0050:UT-09")]
    public void End_ShouldOnlyEndGameInProgress()
    {
        var game = new ChessGame();
        var first = new ChessResult(ChessOutcome.BlackWins, ChessEndReason.Timeout);

        game.End(first);
        game.End(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Resignation));

        Assert.Equal(first, game.Result);
        Assert.True(game.IsOver);
        Assert.False(game.TryPlay(Square.Parse("e2"), Square.Parse("e4"), null, out var played));
        Assert.Null(played);
        Assert.Empty(game.Moves);
    }

    [Fact(DisplayName = "SPEC-0050:UT-09 — End depois de fim por regras não sobrescreve o resultado")]
    [Trait("Category", "SPEC-0050:UT-09")]
    public void End_AfterRulesEnd_ShouldKeepResult()
    {
        var game = new ChessGame();
        SanReplay.Line(game, "f2f3", "e7e5", "g2g4", "d8h4");

        game.End(new ChessResult(ChessOutcome.WhiteWins, ChessEndReason.Abandon));

        Assert.Equal(new ChessResult(ChessOutcome.BlackWins, ChessEndReason.Checkmate), game.Result);
        Assert.False(game.TryPlay(Square.Parse("a2"), Square.Parse("a3"), null, out _));
    }
}
