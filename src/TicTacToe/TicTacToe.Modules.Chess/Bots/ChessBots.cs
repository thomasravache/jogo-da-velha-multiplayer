namespace TicTacToe.Modules.Chess;

/// <summary>Fábrica dos robôs de xadrez.</summary>
public static class ChessBots
{
    /// <summary>
    /// Cria o robô do nível. Mesma semente e mesma posição dão o mesmo lance;
    /// <paramref name="maxNodes"/> só limita o nível Médio.
    /// </summary>
    public static IChessBot Create(ChessBotLevel level, int? seed = null, int? maxNodes = null) => level switch
    {
        ChessBotLevel.Easy => new EasyChessBot(seed),
        ChessBotLevel.Medium => new MediumChessBot(seed, maxNodes),
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };

    public static string NameOf(ChessBotLevel level) => level switch
    {
        ChessBotLevel.Easy => "Robô Fácil 🤖",
        ChessBotLevel.Medium => "Robô Médio 🤖",
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };
}
