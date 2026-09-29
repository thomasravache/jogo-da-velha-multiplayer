using Bunit;
using TicTacToe.Modules.Chess;
using TicTacToe.Web.Components.Chess;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0055: peças SVG do xadrez

public class ChessPieceTests
{
    public static TheoryData<PieceColor, PieceType, string, string> AllPieces => new()
    {
        { PieceColor.White, PieceType.King, "Rei branco", "wK" },
        { PieceColor.White, PieceType.Queen, "Dama branca", "wQ" },
        { PieceColor.White, PieceType.Rook, "Torre branca", "wR" },
        { PieceColor.White, PieceType.Bishop, "Bispo branco", "wB" },
        { PieceColor.White, PieceType.Knight, "Cavalo branco", "wN" },
        { PieceColor.White, PieceType.Pawn, "Peão branco", "wP" },
        { PieceColor.Black, PieceType.King, "Rei preto", "bK" },
        { PieceColor.Black, PieceType.Queen, "Dama preta", "bQ" },
        { PieceColor.Black, PieceType.Rook, "Torre preta", "bR" },
        { PieceColor.Black, PieceType.Bishop, "Bispo preto", "bB" },
        { PieceColor.Black, PieceType.Knight, "Cavalo preto", "bN" },
        { PieceColor.Black, PieceType.Pawn, "Peão preto", "bP" },
    };

    [Theory(DisplayName = "SPEC-0055:UT-01 — ChessPiece renderiza svg role=img com título em português e cores do conjunto")]
    [Trait("Category", "SPEC-0055:UT-01")]
    [MemberData(nameof(AllPieces))]
    public void ChessPiece_ShouldRenderSvgWithPortugueseTitle(PieceColor color, PieceType type, string title, string code)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ChessPiece>(p => p.Add(c => c.Color, color).Add(c => c.Type, type));

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("0 0 64 64", svg.GetAttribute("viewBox"));
        Assert.Equal(code, svg.GetAttribute("data-piece"));
        Assert.Equal(title, cut.Find("svg > title").TextContent);
        Assert.Equal(title, ChessPiece.Title(color, type));

        var side = color == PieceColor.White ? "white" : "black";
        var other = color == PieceColor.White ? "black" : "white";
        Assert.Contains($"stroke-piece-{side}-outline", cut.Markup);
        Assert.Contains($"fill-piece-{side}-fill", cut.Markup);
        Assert.DoesNotContain($"piece-{other}", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0055:UT-01 — ChessPiece usa Size em largura e altura e aceita atributos extras")]
    [Trait("Category", "SPEC-0055:UT-01")]
    public void ChessPiece_ShouldApplySizeAndExtraAttributes()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ChessPiece>(p => p
            .Add(c => c.Color, PieceColor.White)
            .Add(c => c.Type, PieceType.Pawn)
            .Add(c => c.Size, 24)
            .AddUnmatched("class", "size-full"));

        var svg = cut.Find("svg");
        Assert.Equal("24", svg.GetAttribute("width"));
        Assert.Equal("24", svg.GetAttribute("height"));
        Assert.Equal("size-full", svg.GetAttribute("class"));
    }

    [Fact(DisplayName = "SPEC-0055:UT-01 — ChessPiece padrão mede 40")]
    [Trait("Category", "SPEC-0055:UT-01")]
    public void ChessPiece_DefaultSizeShouldBe40()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ChessPiece>(p => p.Add(c => c.Color, PieceColor.Black).Add(c => c.Type, PieceType.Knight));

        Assert.Equal("40", cut.Find("svg").GetAttribute("width"));
    }
}
