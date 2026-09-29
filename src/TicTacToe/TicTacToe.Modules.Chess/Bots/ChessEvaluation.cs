namespace TicTacToe.Modules.Chess;

/// <summary>Avaliação em centipeões, do ponto de vista de quem joga (material e tabelas de casas).</summary>
public static class ChessEvaluation
{
    public const int MateScore = 100_000;

    // Valores por PieceType (peão, cavalo, bispo, torre, dama, rei).
    private static readonly int[] PieceValues = [100, 320, 330, 500, 900, 0];

    // Tabelas de casas do ponto de vista das brancas; linha 0 = 8ª fileira (estilo "Simplified Evaluation Function").
    private static readonly int[][] Tables =
    [
        // Peão
        [
            0, 0, 0, 0, 0, 0, 0, 0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
            5, 5, 10, 25, 25, 10, 5, 5,
            0, 0, 0, 20, 20, 0, 0, 0,
            5, -5, -10, 0, 0, -10, -5, 5,
            5, 10, 10, -20, -20, 10, 10, 5,
            0, 0, 0, 0, 0, 0, 0, 0,
        ],

        // Cavalo
        [
            -50, -40, -30, -30, -30, -30, -40, -50,
            -40, -20, 0, 0, 0, 0, -20, -40,
            -30, 0, 10, 15, 15, 10, 0, -30,
            -30, 5, 15, 20, 20, 15, 5, -30,
            -30, 0, 15, 20, 20, 15, 0, -30,
            -30, 5, 10, 15, 15, 10, 5, -30,
            -40, -20, 0, 5, 5, 0, -20, -40,
            -50, -40, -30, -30, -30, -30, -40, -50,
        ],

        // Bispo
        [
            -20, -10, -10, -10, -10, -10, -10, -20,
            -10, 0, 0, 0, 0, 0, 0, -10,
            -10, 0, 5, 10, 10, 5, 0, -10,
            -10, 5, 5, 10, 10, 5, 5, -10,
            -10, 0, 10, 10, 10, 10, 0, -10,
            -10, 10, 10, 10, 10, 10, 10, -10,
            -10, 5, 0, 0, 0, 0, 5, -10,
            -20, -10, -10, -10, -10, -10, -10, -20,
        ],

        // Torre
        [
            0, 0, 0, 0, 0, 0, 0, 0,
            5, 10, 10, 10, 10, 10, 10, 5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            0, 0, 0, 5, 5, 0, 0, 0,
        ],

        // Dama
        [
            -20, -10, -10, -5, -5, -10, -10, -20,
            -10, 0, 0, 0, 0, 0, 0, -10,
            -10, 0, 5, 5, 5, 5, 0, -10,
            -5, 0, 5, 5, 5, 5, 0, -5,
            0, 0, 5, 5, 5, 5, 0, -5,
            -10, 5, 5, 5, 5, 5, 0, -10,
            -10, 0, 5, 0, 0, 0, 0, -10,
            -20, -10, -10, -5, -5, -10, -10, -20,
        ],

        // Rei (meio-jogo)
        [
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
            20, 20, 0, 0, 0, 0, 20, 20,
            20, 30, 10, 0, 0, 10, 30, 20,
        ],
    ];

    private static readonly int[] KingEndgameTable =
    [
        -50, -40, -30, -20, -20, -30, -40, -50,
        -30, -20, -10, 0, 0, -10, -20, -30,
        -30, -10, 20, 30, 30, 20, -10, -30,
        -30, -10, 30, 40, 40, 30, -10, -30,
        -30, -10, 30, 40, 40, 30, -10, -30,
        -30, -10, 20, 30, 30, 20, -10, -30,
        -30, -30, 0, 0, 0, 0, -30, -30,
        -50, -30, -30, -30, -30, -30, -30, -50,
    ];

    /// <summary>
    /// Avaliação completa: xeque-mate contra quem joga vale -<see cref="MateScore"/>, afogamento vale 0;
    /// nos demais casos, a avaliação estática.
    /// </summary>
    public static int Evaluate(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);

        if (position.LegalMoves().Count == 0)
        {
            return position.IsInCheck(position.SideToMove) ? -MateScore : 0;
        }

        return Static(position);
    }

    /// <summary>Material + tabelas de casas, do ponto de vista de quem joga, sem detectar fim de partida.</summary>
    internal static int Static(Position position)
    {
        var score = 0; // do ponto de vista das brancas
        var queens = 0;
        var whiteKing = 0;
        var blackKing = 0;
        for (var i = 0; i < 64; i++)
        {
            if (position.PieceAt(new Square(i)) is not { } piece)
            {
                continue;
            }

            var type = (int)piece.Type;
            var white = piece.Color == PieceColor.White;
            if (piece.Type == PieceType.King)
            {
                if (white)
                {
                    whiteKing = i;
                }
                else
                {
                    blackKing = i;
                }

                continue;
            }

            if (piece.Type == PieceType.Queen)
            {
                queens++;
            }

            var value = PieceValues[type] + Tables[type][TableIndex(i, white)];
            score += white ? value : -value;
        }

        var kingTable = queens == 0 ? KingEndgameTable : Tables[(int)PieceType.King];
        score += kingTable[TableIndex(whiteKing, true)] - kingTable[TableIndex(blackKing, false)];

        return position.SideToMove == PieceColor.White ? score : -score;
    }

    internal static int ValueOf(PieceType type) => PieceValues[(int)type];

    // As tabelas listam a 8ª fileira primeiro: brancas usam (7 - fileira); pretas espelham a fileira.
    private static int TableIndex(int square, bool white)
    {
        var rank = square >> 3;
        var file = square & 7;
        return ((white ? 7 - rank : rank) * 8) + file;
    }
}
