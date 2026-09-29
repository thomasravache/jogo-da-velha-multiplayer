namespace TicTacToe.Modules.Chess;

/// <summary>Nível Fácil: 20% dos lances aleatórios; nos demais, o melhor lance de profundidade 1.</summary>
public sealed class EasyChessBot : IChessBot
{
    private const double RandomMoveChance = 0.2;

    private readonly Random _random;
    private readonly object _lock = new();

    public EasyChessBot(int? seed = null) =>
        _random = seed is { } value ? new Random(value) : new Random();

    public string Name => ChessBots.NameOf(ChessBotLevel.Easy);

    public Task<Move?> ChooseMoveAsync(Position position, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(position);
        return Task.Run(() => Choose(position), CancellationToken.None);
    }

    private Move? Choose(Position position)
    {
        var moves = position.LegalMoves();
        if (moves.Count == 0)
        {
            return null;
        }

        lock (_lock)
        {
            if (_random.NextDouble() < RandomMoveChance)
            {
                return moves[_random.Next(moves.Count)];
            }

            var bestScore = int.MinValue;
            var candidates = new List<Move>();
            foreach (var move in moves)
            {
                var score = -ChessEvaluation.Evaluate(position.Apply(move));
                if (score > bestScore)
                {
                    bestScore = score;
                    candidates.Clear();
                }

                if (score == bestScore)
                {
                    candidates.Add(move);
                }
            }

            return candidates[_random.Next(candidates.Count)];
        }
    }
}
