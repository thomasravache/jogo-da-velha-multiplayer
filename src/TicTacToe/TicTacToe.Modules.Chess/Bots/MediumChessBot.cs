namespace TicTacToe.Modules.Chess;

/// <summary>
/// Nível Médio: negamax com poda alfa-beta, aprofundamento até a profundidade 3, capturas primeiro
/// e teto de nós. Ao cancelar (ou estourar o teto) devolve o melhor lance da última profundidade completa.
/// </summary>
public sealed class MediumChessBot : IChessBot
{
    public const int DefaultMaxNodes = 200_000;

    private const int MaxDepth = 3;
    private const int MaxPly = 8; // limite da extensão por xeque no horizonte
    private const int Infinity = MateScore + 1_000;
    private const int MateScore = ChessEvaluation.MateScore;

    private readonly Random _random;
    private readonly int _maxNodes;
    private readonly object _lock = new();

    public MediumChessBot(int? seed = null, int? maxNodes = null)
    {
        _random = seed is { } value ? new Random(value) : new Random();
        _maxNodes = maxNodes ?? DefaultMaxNodes;
    }

    public string Name => ChessBots.NameOf(ChessBotLevel.Medium);

    /// <summary>Nós avaliados na última busca (para testes).</summary>
    public long LastNodeCount { get; private set; }

    public Task<Move?> ChooseMoveAsync(Position position, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(position);
        int callSeed;
        lock (_lock)
        {
            callSeed = _random.Next();
        }

        // O token só é observado pela busca (que devolve o melhor lance até então), nunca pelo Task.Run.
        return Task.Run(() => Search(position, new Random(callSeed), ct), CancellationToken.None);
    }

    private Move? Search(Position root, Random random, CancellationToken ct)
    {
        var legal = root.LegalMoves();
        if (legal.Count == 0)
        {
            LastNodeCount = 0;
            return null;
        }

        var search = new SearchState(_maxNodes, ct);
        var ordered = Shuffle(legal, random);
        ordered = Order(root, ordered);
        var best = ordered[0];

        for (var depth = 1; depth <= MaxDepth && legal.Count > 1; depth++)
        {
            var depthBest = ordered[0];
            var alpha = -Infinity;
            var completed = true;
            foreach (var move in ordered)
            {
                var score = -search.Negamax(root.Apply(move), depth - 1, -Infinity, -alpha, 1);
                if (search.Aborted)
                {
                    completed = false;
                    break;
                }

                if (score > alpha)
                {
                    alpha = score;
                    depthBest = move;
                }
            }

            if (!completed)
            {
                break;
            }

            best = depthBest;
            ordered = [best, .. ordered.Where(m => m != best)];
            if (alpha >= MateScore - 100)
            {
                break; // mate forçado encontrado
            }
        }

        LastNodeCount = search.Nodes;
        return best;
    }

    private static Move[] Shuffle(IReadOnlyList<Move> moves, Random random)
    {
        var array = new Move[moves.Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = moves[i];
        }

        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }

        return array;
    }

    // Ordenação estável (a ordem embaralhada desempata): promoções e capturas de maior valor primeiro.
    private static Move[] Order(Position position, Move[] moves)
    {
        var keys = new int[moves.Length];
        for (var i = 0; i < moves.Length; i++)
        {
            keys[i] = -(OrderScore(position, moves[i]) * 1024) + i;
        }

        Array.Sort(keys, moves);
        return moves;
    }

    private static int OrderScore(Position position, Move move)
    {
        var score = 0;
        var mover = position.PieceAt(move.From)!.Value.Type;
        if (position.PieceAt(move.To) is { } victim)
        {
            score = 10 + (ChessEvaluation.ValueOf(victim.Type) / 10) - (ChessEvaluation.ValueOf(mover) / 100);
        }
        else if (mover == PieceType.Pawn && move.From.File != move.To.File)
        {
            score = 20; // en passant
        }

        if (move.Promotion is { } promotion)
        {
            score += ChessEvaluation.ValueOf(promotion) / 10;
        }

        return Math.Clamp(score, 0, 500);
    }

    private sealed class SearchState(int maxNodes, CancellationToken ct)
    {
        public long Nodes { get; private set; }

        public bool Aborted { get; private set; }

        public int Negamax(Position position, int depth, int alpha, int beta, int ply)
        {
            if (Aborted || ct.IsCancellationRequested || Nodes >= maxNodes)
            {
                Aborted = true;
                return 0;
            }

            Nodes++;

            if (position.HalfmoveClock >= 100)
            {
                return 0;
            }

            var side = position.SideToMove;
            var inCheck = position.IsInCheck(side);
            if (depth <= 0)
            {
                if (!inCheck || ply >= MaxPly)
                {
                    return ChessEvaluation.Static(position);
                }

                depth = 1; // estende posições em xeque no horizonte para não perder mates
            }

            var moves = position.LegalMoves();
            if (moves.Count == 0)
            {
                return inCheck ? -(MateScore - ply) : 0;
            }

            var ordered = Order(position, [.. moves]);
            var best = -Infinity;
            foreach (var move in ordered)
            {
                var score = -Negamax(position.Apply(move), depth - 1, -beta, -alpha, ply + 1);
                if (Aborted)
                {
                    return 0;
                }

                if (score > best)
                {
                    best = score;
                }

                if (score > alpha)
                {
                    alpha = score;
                }

                if (alpha >= beta)
                {
                    break;
                }
            }

            return best;
        }
    }
}
