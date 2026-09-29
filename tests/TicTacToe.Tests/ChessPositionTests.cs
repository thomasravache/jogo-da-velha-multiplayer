using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessPositionTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Theory(DisplayName = "SPEC-0049:UT-02 — FEN válido faz ida e volta idêntica")]
    [Trait("Category", "SPEC-0049:UT-02")]
    [InlineData(StartFen)]
    [InlineData("rnbqkbnr/pppp1ppp/8/8/3Pp3/8/PPP1PPPP/RNBQKBNR b KQkq d3 0 3")]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R w - - 12 40")]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1")]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R b Kq - 3 10")]
    public void FromFen_ToFen_ShouldRoundTrip(string fen)
    {
        var position = Position.FromFen(fen);

        Assert.Equal(fen, position.ToFen());
        Assert.True(Position.TryFromFen(fen, out var viaTry));
        Assert.NotNull(viaTry);
        Assert.Equal(fen, viaTry.ToFen());
    }

    [Fact(DisplayName = "SPEC-0049:UT-02 — Position.Start é a posição inicial padrão")]
    [Trait("Category", "SPEC-0049:UT-02")]
    public void Start_ShouldBeStandardInitialPosition()
    {
        var start = Position.Start;

        Assert.Equal(StartFen, start.ToFen());
        Assert.Equal(PieceColor.White, start.SideToMove);
        Assert.Null(start.EnPassantTarget);
        Assert.Equal(0, start.HalfmoveClock);
        Assert.Equal(1, start.FullmoveNumber);
        Assert.Equal(new Piece(PieceColor.White, PieceType.King), start.PieceAt(Square.Parse("e1")));
        Assert.Equal(new Piece(PieceColor.Black, PieceType.Queen), start.PieceAt(Square.Parse("d8")));
        Assert.Null(start.PieceAt(Square.Parse("e4")));
        Assert.True(start.CanCastle(PieceColor.White, kingSide: true));
        Assert.True(start.CanCastle(PieceColor.White, kingSide: false));
        Assert.True(start.CanCastle(PieceColor.Black, kingSide: true));
        Assert.True(start.CanCastle(PieceColor.Black, kingSide: false));
    }

    [Fact(DisplayName = "SPEC-0049:UT-02 — campos do FEN são expostos (lado, roque, en passant, relógios)")]
    [Trait("Category", "SPEC-0049:UT-02")]
    public void FromFen_ShouldExposeAllFields()
    {
        var position = Position.FromFen("r3k2r/8/8/3pP3/8/8/8/R3K2R w Kq d6 5 17");

        Assert.Equal(PieceColor.White, position.SideToMove);
        Assert.True(position.CanCastle(PieceColor.White, kingSide: true));
        Assert.False(position.CanCastle(PieceColor.White, kingSide: false));
        Assert.False(position.CanCastle(PieceColor.Black, kingSide: true));
        Assert.True(position.CanCastle(PieceColor.Black, kingSide: false));
        Assert.Equal(Square.Parse("d6"), position.EnPassantTarget);
        Assert.Equal(5, position.HalfmoveClock);
        Assert.Equal(17, position.FullmoveNumber);
        Assert.Equal(new Piece(PieceColor.Black, PieceType.Pawn), position.PieceAt(Square.Parse("d5")));
        Assert.Equal(new Piece(PieceColor.White, PieceType.Pawn), position.PieceAt(Square.Parse("e5")));
    }

    [Theory(DisplayName = "SPEC-0049:UT-02 — FEN inválido é rejeitado sem exceção inesperada")]
    [Trait("Category", "SPEC-0049:UT-02")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -")]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 extra")]
    [InlineData("8/8/8/8/8/8/8/8 w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/K3K3 w - - 0 1")]
    [InlineData("4k3/4k3/8/8/8/8/8/4K3 w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/P3K3 w - - 0 1")]
    [InlineData("P3k3/8/8/8/8/8/8/4K3 w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/p3K3 w - - 0 1")]
    [InlineData("4k4/8/8/8/8/8/8/4K3 w - - 0 1")]
    [InlineData("3k4/8/8/8/8/8/8/4K3 w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/4K3 w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/8/4K3 w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K2X w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/4R3/K7 w - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 x - - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w KQ - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w Z - 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w - e9 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w - e4 0 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w - - x 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w - - -1 1")]
    [InlineData("4k3/8/8/8/8/8/8/4K3 w - - 0 0")]
    [InlineData("\0\0\0")]
    public void FromFen_ShouldRejectInvalidFen(string fen)
    {
        Assert.Throws<FormatException>(() => Position.FromFen(fen));

        var ok = Position.TryFromFen(fen, out var position);

        Assert.False(ok);
        Assert.Null(position);
    }

    [Fact(DisplayName = "SPEC-0049:UT-02 — TryFromFen com null não lança")]
    [Trait("Category", "SPEC-0049:UT-02")]
    public void TryFromFen_ShouldHandleNull()
    {
        Assert.False(Position.TryFromFen(null!, out var position));
        Assert.Null(position);
    }
}
