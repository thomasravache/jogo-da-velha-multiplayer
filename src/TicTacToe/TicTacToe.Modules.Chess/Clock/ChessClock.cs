namespace TicTacToe.Modules.Chess;

/// <summary>
/// Relógio de xadrez sem threads próprias: o tempo restante é calculado sob demanda a partir do
/// <see cref="TimeProvider"/>. Começa parado; o primeiro <see cref="Press"/> (brancas) inicia o relógio das pretas.
/// Thread-safe: cada operação lê o relógio uma única vez e roda sob um lock privado.
/// </summary>
public sealed class ChessClock(TimeControl control, TimeProvider time)
{
    private readonly Lock _gate = new();
    private TimeSpan _white = control.Initial;
    private TimeSpan _black = control.Initial;
    private DateTimeOffset _startedAt;
    private PieceColor? _running;
    private PieceColor? _flagged;
    private bool _stopped;

    public PieceColor? Running
    {
        get
        {
            lock (_gate)
            {
                Evaluate(time.GetUtcNow());
                return _running;
            }
        }
    }

    public PieceColor? Flagged
    {
        get
        {
            lock (_gate)
            {
                Evaluate(time.GetUtcNow());
                return _flagged;
            }
        }
    }

    public void Press(PieceColor mover)
    {
        lock (_gate)
        {
            var now = time.GetUtcNow();
            Evaluate(now);
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
                Start(PieceColor.Black, now);
                return;
            }

            if (_running != mover)
            {
                return;
            }

            Set(mover, RemainingAt(mover, now) + control.Increment);
            Start(Opponent(mover), now);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            var now = time.GetUtcNow();
            Evaluate(now);
            if (_running is { } running)
            {
                Set(running, RemainingAt(running, now));
            }

            _running = null;
            _stopped = true;
        }
    }

    public TimeSpan Remaining(PieceColor color)
    {
        lock (_gate)
        {
            return RemainingAt(color, time.GetUtcNow());
        }
    }

    public void Tick()
    {
        lock (_gate)
        {
            Evaluate(time.GetUtcNow());
        }
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

    private TimeSpan RemainingAt(PieceColor color, DateTimeOffset now)
    {
        var stored = Get(color);
        if (_running != color)
        {
            return stored;
        }

        var left = stored - (now - _startedAt);
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    private void Evaluate(DateTimeOffset now)
    {
        if (_running is not { } running || RemainingAt(running, now) > TimeSpan.Zero)
        {
            return;
        }

        Set(running, TimeSpan.Zero);
        _flagged = running;
        _running = null;
    }

    private void Start(PieceColor color, DateTimeOffset now)
    {
        _running = color;
        _startedAt = now;
    }
}
