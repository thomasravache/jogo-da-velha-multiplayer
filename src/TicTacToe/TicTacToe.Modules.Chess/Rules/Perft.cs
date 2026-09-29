namespace TicTacToe.Modules.Chess;

public static class Perft
{
    public static long Count(Position position, int depth)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);

        return position.PerftCount(depth);
    }
}
