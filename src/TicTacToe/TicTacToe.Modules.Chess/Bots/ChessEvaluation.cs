namespace TicTacToe.Modules.Chess;

/// <summary>Avaliação estática em centipeões, do ponto de vista de quem joga.</summary>
public static class ChessEvaluation
{
    public const int MateScore = 100_000;

    public static int Evaluate(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);
        throw new NotImplementedException();
    }
}
