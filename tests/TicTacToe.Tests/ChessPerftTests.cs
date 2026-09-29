using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

/// <summary>
/// Teoria pesada: só roda quando a variável de ambiente CHESS_SLOW=1 está definida (execução local).
/// O CI padrão executa todos os testes, então o perft pesado fica pulado por padrão.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class SlowTheoryAttribute : TheoryAttribute
{
    public SlowTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("CHESS_SLOW") != "1")
        {
            Skip = "Perft pesado (Category=Slow): defina CHESS_SLOW=1 para executar localmente.";
        }
    }
}

public class ChessPerftTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string KiwipeteFen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    private const string Position3Fen = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
    private const string Position4Fen = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
    private const string Position5Fen = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
    private const string Position6Fen = "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10";

    [Theory(DisplayName = "SPEC-0049:IT-01 — perft de profundidade padrão bate com as contagens publicadas")]
    [Trait("Category", "SPEC-0049:IT-01")]
    [InlineData(StartFen, 0, 1L)]
    [InlineData(StartFen, 1, 20L)]
    [InlineData(StartFen, 2, 400L)]
    [InlineData(StartFen, 3, 8_902L)]
    [InlineData(StartFen, 4, 197_281L)]
    [InlineData(KiwipeteFen, 1, 48L)]
    [InlineData(KiwipeteFen, 2, 2_039L)]
    [InlineData(KiwipeteFen, 3, 97_862L)]
    [InlineData(Position3Fen, 1, 14L)]
    [InlineData(Position3Fen, 2, 191L)]
    [InlineData(Position3Fen, 3, 2_812L)]
    [InlineData(Position4Fen, 1, 6L)]
    [InlineData(Position4Fen, 2, 264L)]
    [InlineData(Position4Fen, 3, 9_467L)]
    [InlineData(Position5Fen, 1, 44L)]
    [InlineData(Position5Fen, 2, 1_486L)]
    [InlineData(Position5Fen, 3, 62_379L)]
    [InlineData(Position6Fen, 1, 46L)]
    [InlineData(Position6Fen, 2, 2_079L)]
    [InlineData(Position6Fen, 3, 89_890L)]
    public void Perft_DefaultDepth(string fen, int depth, long expected) =>
        Assert.Equal(expected, Perft.Count(Position.FromFen(fen), depth));

    [SlowTheory(DisplayName = "SPEC-0049:IT-01b — perft pesado bate com as contagens publicadas")]
    [Trait("Category", "SPEC-0049:IT-01b")]
    [Trait("Category", "Slow")]
    [InlineData(StartFen, 5, 4_865_609L)]
    [InlineData(KiwipeteFen, 4, 4_085_603L)]
    [InlineData(Position3Fen, 4, 43_238L)]
    [InlineData(Position3Fen, 5, 674_624L)]
    [InlineData(Position4Fen, 4, 422_333L)]
    [InlineData(Position5Fen, 4, 2_103_487L)]
    [InlineData(Position6Fen, 4, 3_894_594L)]
    public void Perft_HeavyDepth(string fen, int depth, long expected) =>
        Assert.Equal(expected, Perft.Count(Position.FromFen(fen), depth));

    [Fact(DisplayName = "SPEC-0049:IT-01 — profundidade negativa é rejeitada")]
    [Trait("Category", "SPEC-0049:IT-01")]
    public void Perft_NegativeDepth_ShouldThrow() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Perft.Count(Position.Start, -1));
}
