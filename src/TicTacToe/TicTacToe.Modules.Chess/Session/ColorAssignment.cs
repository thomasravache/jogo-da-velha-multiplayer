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
        (first, second) switch
        {
            (ColorPreference.White, ColorPreference.Black or ColorPreference.Random) => PieceColor.White,
            (ColorPreference.Black, ColorPreference.White or ColorPreference.Random) => PieceColor.Black,
            (ColorPreference.Random, ColorPreference.White) => PieceColor.Black,
            (ColorPreference.Random, ColorPreference.Black) => PieceColor.White,
            _ => coinFlip() ? PieceColor.White : PieceColor.Black,
        };
}
