using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TicTacToe.Tests")]

namespace TicTacToe.Modules.Chess;

/// <summary>
/// Núcleo da sessão de xadrez compartilhada por dois circuitos: assentos, lances, relógio, resultado e snapshot.
/// Todo comando e o <see cref="Snapshot"/> rodam sob um único lock; <see cref="OnStateChanged"/> nunca é disparado dentro dele.
/// O ciclo de vida (abandono, revanche, presença) fica em ChessSession.Lifecycle.cs (SPEC-0061).
/// </summary>
public sealed partial class ChessSession : IDisposable
{
    private readonly Lock _gate = new();
    private readonly TimeProvider _time;
    private readonly Position? _start;
    private readonly ITimer? _timer;
    private readonly string[] _names = ["", ""];
    private readonly Guid?[] _playerIds = [null, null];
    private readonly PieceColor?[] _colors = [null, null];
    private ChessGame _game;
    private ChessClock _clock;
    private ChessMode _mode;
    private DateTimeOffset _startedAt;
    private DateTimeOffset? _endedAt;
    private bool _recorded;

    public ChessSession(TimeControl control, TimeProvider? timeProvider = null, bool enableBackgroundTimer = true, Position? start = null)
    {
        Control = control;
        _time = timeProvider ?? TimeProvider.System;
        _start = start;
        _game = new ChessGame(start);
        _clock = new ChessClock(control, _time);
        _startedAt = _time.GetUtcNow();
        if (enableBackgroundTimer)
        {
            _timer = _time.CreateTimer(_ => Tick(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    public event Action? OnStateChanged;

    public Guid Id { get; } = Guid.NewGuid();

    public TimeControl Control { get; }

    public ChessMode Mode
    {
        get
        {
            lock (_gate)
            {
                return _mode;
            }
        }

        set
        {
            lock (_gate)
            {
                _mode = value;
            }
        }
    }

    public ChessResult? Result
    {
        get
        {
            lock (_gate)
            {
                return _game.Result;
            }
        }
    }

    public bool IsOver => Result is not null;

    public DateTimeOffset StartedAtUtc
    {
        get
        {
            lock (_gate)
            {
                return _startedAt;
            }
        }
    }

    public DateTimeOffset? EndedAtUtc
    {
        get
        {
            lock (_gate)
            {
                return _endedAt;
            }
        }
    }

    public TimeSpan? Duration
    {
        get
        {
            lock (_gate)
            {
                return _endedAt - _startedAt;
            }
        }
    }

    public void SetSeat(int seat, string name, Guid? playerId, PieceColor color)
    {
        ValidateSeat(seat);
        ArgumentNullException.ThrowIfNull(name);
        lock (_gate)
        {
            if (_colors[1 - seat] == color)
            {
                throw new InvalidOperationException("Os dois assentos não podem ter a mesma cor.");
            }

            _names[seat] = name;
            _playerIds[seat] = playerId;
            _colors[seat] = color;
        }

        RaiseStateChanged();
    }

    public PieceColor ColorOf(int seat)
    {
        ValidateSeat(seat);
        lock (_gate)
        {
            return ColorOfLocked(seat);
        }
    }

    public int SeatOf(PieceColor color)
    {
        lock (_gate)
        {
            return SeatOfLocked(color);
        }
    }

    public string GetSeatName(int seat)
    {
        ValidateSeat(seat);
        lock (_gate)
        {
            return _names[seat];
        }
    }

    public Guid? GetSeatPlayerId(int seat)
    {
        ValidateSeat(seat);
        lock (_gate)
        {
            return _playerIds[seat];
        }
    }

    public string GetPlayerName(PieceColor color)
    {
        lock (_gate)
        {
            return _names[SeatOfLocked(color)];
        }
    }

    public Guid? GetPlayerId(PieceColor color)
    {
        lock (_gate)
        {
            return _playerIds[SeatOfLocked(color)];
        }
    }

    public bool TryMove(PieceColor player, Square from, Square to, PieceType? promotion, out ChessMove? played)
    {
        bool changed;
        bool accepted;
        lock (_gate)
        {
            (accepted, played, changed) = TryMoveLocked(player, from, to, promotion);
        }

        if (changed)
        {
            RaiseStateChanged();
        }

        return accepted;
    }

    public ChessSnapshot Snapshot()
    {
        ChessSnapshot snapshot;
        bool changed;
        lock (_gate)
        {
            changed = ApplyFlagLocked();
            var position = _game.Position;
            snapshot = new ChessSnapshot(
                Id,
                _mode,
                Control,
                position,
                [.. _game.Moves],
                _game.CapturedBy(PieceColor.White),
                _game.CapturedBy(PieceColor.Black),
                _clock.Remaining(PieceColor.White),
                _clock.Remaining(PieceColor.Black),
                _clock.Running,
                position.SideToMove,
                _game.Result,
                _names[SeatOfLocked(PieceColor.White)],
                _names[SeatOfLocked(PieceColor.Black)],
                _startedAt,
                _endedAt);
        }

        if (changed)
        {
            RaiseStateChanged();
        }

        return snapshot;
    }

    public void Tick()
    {
        bool changed;
        lock (_gate)
        {
            changed = ApplyFlagLocked();
        }

        if (changed)
        {
            RaiseStateChanged();
        }

        TickLifecycle();
    }

    public bool TryMarkResultRecorded()
    {
        lock (_gate)
        {
            if (_game.Result is null || _recorded)
            {
                return false;
            }

            _recorded = true;
            return true;
        }
    }

    public void Dispose() => _timer?.Dispose();

    /// <summary>Nova partida de <c>start ?? Position.Start</c> com relógio novo; troca as cores dos assentos se pedido.</summary>
    internal bool RestartCore(bool swapColors)
    {
        lock (_gate)
        {
            if (swapColors)
            {
                var white = SeatOfLocked(PieceColor.White);
                var black = SeatOfLocked(PieceColor.Black);
                _colors[white] = PieceColor.Black;
                _colors[black] = PieceColor.White;
            }

            _game = new ChessGame(_start);
            _clock = new ChessClock(Control, _time);
            _startedAt = _time.GetUtcNow();
            _endedAt = null;
            _recorded = false;
        }

        RaiseStateChanged();
        return true;
    }

    partial void TickLifecycle();

    private static void ValidateSeat(int seat) => ArgumentOutOfRangeException.ThrowIfNotEqual((uint)seat < 2u, true, nameof(seat));

    private static PieceColor Opposite(PieceColor color) => color == PieceColor.White ? PieceColor.Black : PieceColor.White;

    private PieceColor ColorOfLocked(int seat)
    {
        if (_colors[seat] is { } color)
        {
            return color;
        }

        return _colors[1 - seat] is { } other ? Opposite(other) : seat == 0 ? PieceColor.White : PieceColor.Black;
    }

    private int SeatOfLocked(PieceColor color) => ColorOfLocked(0) == color ? 0 : 1;

    private (bool Accepted, ChessMove? Played, bool Changed) TryMoveLocked(PieceColor player, Square from, Square to, PieceType? promotion)
    {
        if (ApplyFlagLocked())
        {
            return (false, null, true);
        }

        if (_game.IsOver || player != _game.Position.SideToMove || !_game.TryPlay(from, to, promotion, out var played))
        {
            return (false, null, false);
        }

        _clock.Press(player);
        if (_game.IsOver)
        {
            EndLocked();
        }
        else
        {
            ApplyFlagLocked();
        }

        return (true, played, true);
    }

    /// <summary>Reavalia a bandeira; se caiu, encerra por tempo (empate se o vencedor não tem material de mate).</summary>
    private bool ApplyFlagLocked()
    {
        if (_game.IsOver || _clock.Flagged is not { } flagged)
        {
            return false;
        }

        var winner = Opposite(flagged);
        var outcome = !_game.HasMatingMaterial(winner)
            ? ChessOutcome.Draw
            : winner == PieceColor.White ? ChessOutcome.WhiteWins : ChessOutcome.BlackWins;
        _game.End(new ChessResult(outcome, ChessEndReason.Timeout));
        EndLocked();
        return true;
    }

    private void EndLocked()
    {
        _clock.Stop();
        _endedAt ??= _time.GetUtcNow();
    }

    private void RaiseStateChanged() => OnStateChanged?.Invoke();
}
