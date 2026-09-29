using System.Text.RegularExpressions;
using Bunit;
using TicTacToe.Modules.Chess;
using TicTacToe.Web.Components.Chess;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0057: lista de lances da arena de xadrez

public class ChessMoveListTests
{
    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static IReadOnlyList<ChessMove> Played(params string[] moves)
    {
        using var session = ChessSessionTests.New();
        ChessSessionTests.Line(session, moves);
        return session.Snapshot().Moves;
    }

    [Fact(DisplayName = "SPEC-0057:UT-05 — 5 lances: '1. e4 e5', '2. Nf3 Nc6', '3. Bc4' em ordem, último marcado como atual")]
    [Trait("Category", "SPEC-0057:UT-05")]
    public void MoveList_ShouldNumberFullMovesAndMarkLast()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ChessMoveList>(p => p.Add(c => c.Moves, Played("e2e4", "e7e5", "g1f3", "b8c6", "f1c4")));

        Assert.Equal("ol", cut.Find("[data-move-list]").TagName.ToLowerInvariant());
        var items = cut.FindAll("[data-move-list] li").Select(li => Normalize(li.TextContent)).ToArray();
        Assert.Equal(["1. e4 e5", "2. Nf3 Nc6", "3. Bc4"], items);
        var current = cut.FindAll("[aria-current]");
        Assert.Single(current);
        Assert.Equal("Bc4", Normalize(current[0].TextContent));
    }

    [Fact(DisplayName = "SPEC-0057:UT-05 — número par de lances marca o lance das pretas como atual")]
    [Trait("Category", "SPEC-0057:UT-05")]
    public void MoveList_ShouldMarkBlackMoveAsCurrent()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ChessMoveList>(p => p.Add(c => c.Moves, Played("e2e4", "e7e5")));

        Assert.Equal("e5", Normalize(cut.Find("[aria-current]").TextContent));
        Assert.Single(cut.FindAll("[data-move-list] li"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-05 — sem lances a lista fica vazia e sem item atual")]
    [Trait("Category", "SPEC-0057:UT-05")]
    public void MoveList_ShouldBeEmptyWithoutMoves()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ChessMoveList>();

        Assert.Empty(cut.FindAll("[data-move-list] li"));
        Assert.Empty(cut.FindAll("[aria-current]"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-05 — centenas de lances renderizam todos os itens")]
    [Trait("Category", "SPEC-0057:UT-05")]
    public void MoveList_ShouldRenderHundredsOfMoves()
    {
        using var ctx = new BunitContext();
        var move = new ChessMove(new Move(Square.Parse("e2"), Square.Parse("e4")), "e4", PieceType.Pawn, null, false, false);

        var cut = ctx.Render<ChessMoveList>(p => p.Add(c => c.Moves, Enumerable.Repeat(move, 401).ToArray()));

        Assert.Equal(201, cut.FindAll("[data-move-list] li").Count);
    }
}
