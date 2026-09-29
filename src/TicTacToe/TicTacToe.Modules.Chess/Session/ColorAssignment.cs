namespace TicTacToe.Modules.Chess;

public enum ColorPreference
{
    Random,
    White,
    Black,
}

/// <summary>Regras de atribuição de cor ao parear dois jogadores (SPEC-0053).</summary>
public static class ColorAssignment
{
    /// <summary>Cor do primeiro jogador; <paramref name="coinFlip"/> (true = brancas) só é consultado nos empates de preferência.</summary>
    public static PieceColor AssignFirst(ColorPreference first, ColorPreference second, Func<bool> coinFlip) =>
        throw new NotImplementedException();
}
