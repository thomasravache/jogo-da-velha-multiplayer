namespace TicTacToe.Modules.Chess;

public sealed class ChessClock(TimeControl control, TimeProvider time)
{
    private readonly TimeControl _control = control;
    private readonly TimeProvider _time = time;

    public PieceColor? Running { get; private set; }

    public PieceColor? Flagged { get; private set; }

    public void Press(PieceColor mover) => throw new NotImplementedException($"{mover} {_control.Id}");

    public void Stop() => throw new NotImplementedException(_time.ToString());

    public TimeSpan Remaining(PieceColor color) => color == Running ? _control.Initial : TimeSpan.Zero;

    public void Tick() => throw new NotImplementedException();
}
