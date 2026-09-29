using System.Text;
using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessEvaluationTests
{
    private const string MiddlegameFen = "r1bq1rk1/pp2bppp/2n1pn2/2pp4/3P1B2/2PBPN2/PP1N1PPP/R2QK2R w KQ - 0 9";

    /// <summary>Espelha a posição: inverte as fileiras, troca a cor das peças e o lado a jogar.</summary>
    private static string Mirror(string fen)
    {
        var fields = fen.Split(' ');
        var ranks = fields[0].Split('/').Reverse().Select(SwapCase);
        var side = fields[1] == "w" ? "b" : "w";
        var castling = fields[2] == "-" ? "-" : SwapCase(fields[2]);
        var enPassant = fields[3];
        if (enPassant != "-")
        {
            enPassant = string.Concat(enPassant[0], (char)('1' + ('8' - enPassant[1])));
        }

        return $"{string.Join('/', ranks)} {side} {castling} {enPassant} {fields[4]} {fields[5]}";
    }

    private static string SwapCase(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            sb.Append(char.IsUpper(ch) ? char.ToLowerInvariant(ch) : char.ToUpperInvariant(ch));
        }

        return sb.ToString();
    }

    [Fact(DisplayName = "SPEC-0054:UT-01 — material igual avalia ≈ 0")]
    [Trait("Category", "SPEC-0054:UT-01")]
    public void Evaluate_EqualMaterial_IsAroundZero()
    {
        var score = ChessEvaluation.Evaluate(Position.Start);

        Assert.InRange(score, -50, 50);
    }

    [Fact(DisplayName = "SPEC-0054:UT-01 — uma peça a mais é positiva para quem a tem, do ponto de vista de quem joga")]
    [Trait("Category", "SPEC-0054:UT-01")]
    public void Evaluate_ExtraPiece_FavoursOwner()
    {
        var whiteUpWhiteToMove = Position.FromFen("r1bqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        var whiteUpBlackToMove = Position.FromFen("r1bqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1");

        Assert.InRange(ChessEvaluation.Evaluate(whiteUpWhiteToMove), 250, 400);
        Assert.InRange(ChessEvaluation.Evaluate(whiteUpBlackToMove), -400, -250);
    }

    [Fact(DisplayName = "SPEC-0054:UT-01 — mate dado é enorme para o lado que mata e ±100000 para o mated")]
    [Trait("Category", "SPEC-0054:UT-01")]
    public void Evaluate_Checkmate_IsHuge()
    {
        var whiteMated = Position.FromFen("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3");
        var blackMated = Position.FromFen(Mirror("rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3"));

        Assert.Equal(-ChessEvaluation.MateScore, ChessEvaluation.Evaluate(whiteMated));
        Assert.Equal(-ChessEvaluation.MateScore, ChessEvaluation.Evaluate(blackMated));
    }

    [Fact(DisplayName = "SPEC-0054:UT-01 — afogamento vale 0")]
    [Trait("Category", "SPEC-0054:UT-01")]
    public void Evaluate_Stalemate_IsZero()
    {
        var stalemate = Position.FromFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");

        Assert.Equal(0, ChessEvaluation.Evaluate(stalemate));
    }

    [Theory(DisplayName = "SPEC-0054:UT-01 — trocar as cores espelha o sinal (avaliação simétrica)")]
    [Trait("Category", "SPEC-0054:UT-01")]
    [InlineData(MiddlegameFen)]
    [InlineData("r1bqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
    [InlineData("8/5pk1/6p1/8/3P4/4K3/8/8 w - - 0 1")]
    public void Evaluate_ColorSwap_IsSymmetric(string fen)
    {
        var original = ChessEvaluation.Evaluate(Position.FromFen(fen));
        var mirrored = ChessEvaluation.Evaluate(Position.FromFen(Mirror(fen)));

        Assert.Equal(original, mirrored);
    }
}
