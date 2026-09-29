namespace TicTacToe.Modules.Chess;

/// <summary>Nível Médio: negamax com poda alfa-beta, profundidade 3 e teto de nós.</summary>
public sealed class MediumChessBot : IChessBot
{
    public const int DefaultMaxNodes = 200_000;

    public MediumChessBot(int? seed = null, int? maxNodes = null) => _ = (seed, maxNodes);

    public string Name => ChessBots.NameOf(ChessBotLevel.Medium);

    /// <summary>Nós avaliados na última busca (para testes).</summary>
    public long LastNodeCount { get; private set; }

    public Task<Move?> ChooseMoveAsync(Position position, CancellationToken ct) =>
        throw new NotImplementedException();
}
