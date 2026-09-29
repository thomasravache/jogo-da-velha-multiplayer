namespace TicTacToe.Modules.Chess;

public readonly record struct Square(int Index)
{
    public int File => Index % 8;

    public int Rank => Index / 8;

    public static Square Parse(string algebraic) =>
        TryParse(algebraic, out var square)
            ? square
            : throw new FormatException($"Casa inválida: '{algebraic}'.");

    public static bool TryParse(string text, out Square square)
    {
        if (text is { Length: 2 } && text[0] is >= 'a' and <= 'h' && text[1] is >= '1' and <= '8')
        {
            square = new Square((text[1] - '1') * 8 + (text[0] - 'a'));
            return true;
        }

        square = default;
        return false;
    }

    public override string ToString() =>
        string.Create(2, Index, static (span, index) =>
        {
            span[0] = (char)('a' + (index % 8));
            span[1] = (char)('1' + (index / 8));
        });
}
