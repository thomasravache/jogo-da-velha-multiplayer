using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessSquareTests
{
    [Theory(DisplayName = "SPEC-0049:UT-01 — Parse e ToString são inversos e os índices seguem a1 = 0 … h8 = 63")]
    [Trait("Category", "SPEC-0049:UT-01")]
    [InlineData("a1", 0, 0, 0)]
    [InlineData("b1", 1, 1, 0)]
    [InlineData("e4", 28, 4, 3)]
    [InlineData("h1", 7, 7, 0)]
    [InlineData("a8", 56, 0, 7)]
    [InlineData("h8", 63, 7, 7)]
    public void Parse_ShouldMapAlgebraicToIndexAndBack(string text, int index, int file, int rank)
    {
        var square = Square.Parse(text);

        Assert.Equal(index, square.Index);
        Assert.Equal(file, square.File);
        Assert.Equal(rank, square.Rank);
        Assert.Equal(text, square.ToString());
        Assert.True(Square.TryParse(text, out var viaTry));
        Assert.Equal(square, viaTry);
    }

    [Theory(DisplayName = "SPEC-0049:UT-01 — textos inválidos são rejeitados")]
    [Trait("Category", "SPEC-0049:UT-01")]
    [InlineData("i9")]
    [InlineData("")]
    [InlineData("e")]
    [InlineData("e44")]
    [InlineData("e9")]
    [InlineData("E4")]
    [InlineData("i1")]
    [InlineData("a0")]
    public void TryParse_ShouldRejectInvalidText(string text)
    {
        Assert.False(Square.TryParse(text, out _));
        Assert.Throws<FormatException>(() => Square.Parse(text));
    }

    [Fact(DisplayName = "SPEC-0049:UT-01 — todas as 64 casas fazem a ida e volta")]
    [Trait("Category", "SPEC-0049:UT-01")]
    public void AllSquares_ShouldRoundTrip()
    {
        for (var i = 0; i < 64; i++)
        {
            var square = new Square(i);
            Assert.Equal(square, Square.Parse(square.ToString()));
        }
    }
}
