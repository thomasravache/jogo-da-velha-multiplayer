namespace TicTacToe.Modules.Chess;

public readonly record struct Square(int Index)
{
    public int File => Index % 8;

    public int Rank => Index / 8;

    public static Square Parse(string algebraic) => throw new NotImplementedException();

    public static bool TryParse(string text, out Square square) => throw new NotImplementedException();

    public override string ToString() => throw new NotImplementedException();
}
