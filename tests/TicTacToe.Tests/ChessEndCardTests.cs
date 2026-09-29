using Bunit;
using TicTacToe.Modules.Chess;
using TicTacToe.Web.Components.Chess;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0057: cartão de fim de partida

public class ChessEndCardTests
{
    private static IRenderedComponent<ChessEndCard> Render(
        BunitContext ctx, ChessOutcome outcome, ChessEndReason reason, string? winner, int moves = 65, TimeSpan? duration = null) =>
        ctx.Render<ChessEndCard>(p => p
            .Add(c => c.Result, new ChessResult(outcome, reason))
            .Add(c => c.WinnerName, winner)
            .Add(c => c.MoveCount, moves)
            .Add(c => c.Duration, duration ?? TimeSpan.FromSeconds(185)));

    [Theory(DisplayName = "SPEC-0057:UT-08 — cada motivo de fim tem título próprio")]
    [Trait("Category", "SPEC-0057:UT-08")]
    [InlineData(ChessOutcome.WhiteWins, ChessEndReason.Checkmate, "Ana", "Xeque-mate — Ana venceu")]
    [InlineData(ChessOutcome.Draw, ChessEndReason.Stalemate, null, "Empate por afogamento")]
    [InlineData(ChessOutcome.Draw, ChessEndReason.InsufficientMaterial, null, "Empate por material insuficiente")]
    [InlineData(ChessOutcome.Draw, ChessEndReason.FiftyMoveRule, null, "Empate pela regra dos 50 lances")]
    [InlineData(ChessOutcome.Draw, ChessEndReason.ThreefoldRepetition, null, "Empate por repetição")]
    [InlineData(ChessOutcome.BlackWins, ChessEndReason.Timeout, "Bia", "Vitória por tempo — Bia venceu")]
    [InlineData(ChessOutcome.Draw, ChessEndReason.Timeout, null, "Empate por tempo (sem material de mate)")]
    [InlineData(ChessOutcome.WhiteWins, ChessEndReason.Resignation, "Ana", "Vitória por desistência — Ana venceu")]
    [InlineData(ChessOutcome.BlackWins, ChessEndReason.Abandon, "Bia", "Vitória por abandono — Bia venceu")]
    [InlineData(ChessOutcome.WhiteWins, ChessEndReason.Disconnect, "Ana", "Vitória por desconexão — Ana venceu")]
    public void EndCard_ShouldTitleEachReason(ChessOutcome outcome, ChessEndReason reason, string? winner, string title)
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, outcome, reason, winner);

        Assert.Equal(title, cut.Find("[data-end-title]").TextContent.Trim());
    }

    [Fact(DisplayName = "SPEC-0057:UT-08 — resumo mostra lances completos (⌈meios/2⌉) e duração mm:ss")]
    [Trait("Category", "SPEC-0057:UT-08")]
    public void EndCard_ShouldSummarizeMovesAndDuration()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, ChessOutcome.WhiteWins, ChessEndReason.Checkmate, "Ana", moves: 65, duration: TimeSpan.FromSeconds(185));

        var summary = cut.Find("[data-end-summary]").TextContent;
        Assert.Contains("33 lances", summary, StringComparison.Ordinal);
        Assert.Contains("03:05", summary, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "SPEC-0057:UT-08 — singular para 1 lance completo")]
    [Trait("Category", "SPEC-0057:UT-08")]
    [InlineData(1)]
    [InlineData(2)]
    public void EndCard_ShouldUseSingularForOneMove(int halfMoves)
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, ChessOutcome.WhiteWins, ChessEndReason.Resignation, "Ana", moves: halfMoves);

        var summary = cut.Find("[data-end-summary]").TextContent;
        Assert.Contains("1 lance", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("1 lances", summary, StringComparison.Ordinal);
    }
}
