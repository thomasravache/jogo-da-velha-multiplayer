namespace TicTacToe.Modules.Chess;

/// <summary>Nível Fácil: profundidade 1 com 20% de lances aleatórios.</summary>
public sealed class EasyChessBot : IChessBot
{
    public EasyChessBot(int? seed = null) => _ = seed;

    public string Name => ChessBots.NameOf(ChessBotLevel.Easy);

    public Task<Move?> ChooseMoveAsync(Position position, CancellationToken ct) =>
        throw new NotImplementedException();
}
