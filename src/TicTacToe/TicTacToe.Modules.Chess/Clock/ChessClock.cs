namespace TicTacToe.Modules.Chess;

/// <summary>
/// Relógio de xadrez sem threads: o tempo restante é calculado sob demanda a partir do <see cref="TimeProvider"/>.
/// Começa parado; o primeiro <see cref="Press"/> (brancas) inicia o relógio das pretas.
/// </summary>
public sealed class ChessClock(TimeControl control, TimeProvider time)
{
    private TimeSpan _white = control.Initial;
    private TimeSpan _black = control.Initial;
    private DateTimeOffset _startedAt;
    private bool _stopped;

    private PieceColor? _running;
    private PieceColor? _flagged;

    public PieceColor? Running
    {
        get
        {
            Tick();
            return _running;
        }
    }

    public PieceColor? Flagged
    {
        get
        {
            Tick();
            return _flagged;
        }
    }

    public void Press(PieceColor mover)
    {
        Tick();
        if (_stopped || _flagged is not null)
        {
            return;
        }

        if (_running is null)
        {
            if (mover != PieceColor.White)
            {
                return;
            }

            Set(PieceColor.White, Get(PieceColor.White) + control.Increment);
            Start(PieceColor.Black);
            return;
        }

        if (_running != mover)
        {
            return;
        }

        Set(mover, Remaining(mover) + control.Increment);
        Start(Opponent(mover));
    }

    public void Stop()
    {
        Tick();
        if (_running is { } running)
        {
            Set(running, Remaining(running));
        }

        _running = null;
        _stopped = true;
    }

    public TimeSpan Remaining(PieceColor color)
    {
        var stored = Get(color);
        if (_running != color)
        {
            return stored;
        }

        var left = stored - (time.GetUtcNow() - _startedAt);
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    public void Tick()
    {
        if (_running is not { } running || Remaining(running) > TimeSpan.Zero)
        {
            return;
        }

        Set(running, TimeSpan.Zero);
        _flagged = running;
        _running = null;
    }

    private static PieceColor Opponent(PieceColor color) =>
        color == PieceColor.White ? PieceColor.Black : PieceColor.White;

    private TimeSpan Get(PieceColor color) => color == PieceColor.White ? _white : _black;

    private void Set(PieceColor color, TimeSpan value)
    {
        if (color == PieceColor.White)
        {
            _white = value;
        }
        else
        {
            _black = value;
        }
    }

    private void Start(PieceColor color)
    {
        _running = color;
        _startedAt = time.GetUtcNow();
    }
}
