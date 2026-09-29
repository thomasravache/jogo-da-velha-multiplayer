namespace TicTacToe.Modules.Chess;

/// <summary>Regras de material como funções puras.</summary>
internal static class ChessMaterial
{
    public static bool HasMatingMaterial(Position position, PieceColor color)
    {
        var minors = 0;
        for (var index = 0; index < 64; index++)
        {
            if (position.PieceAt(new Square(index)) is not { } piece || piece.Color != color)
            {
                continue;
            }

            switch (piece.Type)
            {
                case PieceType.Pawn or PieceType.Rook or PieceType.Queen:
                    return true;
                case PieceType.Knight or PieceType.Bishop:
                    minors++;
                    break;
            }
        }

        return minors >= 2;
    }

    /// <summary>K x K, K+B x K, K+N x K, ou só bispos todos em casas de mesma cor.</summary>
    public static bool IsInsufficient(Position position)
    {
        var others = 0;
        var knights = 0;
        var lightBishops = 0;
        var darkBishops = 0;
        for (var index = 0; index < 64; index++)
        {
            var square = new Square(index);
            if (position.PieceAt(square) is not { } piece || piece.Type == PieceType.King)
            {
                continue;
            }

            switch (piece.Type)
            {
                case PieceType.Knight:
                    knights++;
                    break;
                case PieceType.Bishop when (square.File + square.Rank) % 2 == 1:
                    lightBishops++;
                    break;
                case PieceType.Bishop:
                    darkBishops++;
                    break;
                default:
                    others++;
                    break;
            }
        }

        if (others > 0)
        {
            return false;
        }

        var bishops = lightBishops + darkBishops;
        if (knights == 0)
        {
            return lightBishops == 0 || darkBishops == 0;
        }

        return knights == 1 && bishops == 0;
    }
}
