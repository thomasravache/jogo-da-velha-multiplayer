namespace TicTacToe.Modules.Chess;

/// <summary>Fábrica dos robôs de xadrez.</summary>
public static class ChessBots
{
    public static IChessBot Create(ChessBotLevel level, int? seed = null, int? maxNodes = null) =>
        throw new NotImplementedException();

    public static string NameOf(ChessBotLevel level) => level switch
    {
        ChessBotLevel.Easy => "Robô Fácil 🤖",
        ChessBotLevel.Medium => "Robô Médio 🤖",
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };
}
