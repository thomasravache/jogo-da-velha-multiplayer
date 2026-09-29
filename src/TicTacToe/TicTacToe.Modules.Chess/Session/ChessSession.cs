using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TicTacToe.Tests")]

namespace TicTacToe.Modules.Chess;

/// <summary>Núcleo da sessão de xadrez compartilhada; o ciclo de vida fica em ChessSession.Lifecycle.cs (SPEC-0061).</summary>
public sealed partial class ChessSession : IDisposable
{
    public ChessSession(TimeControl control, TimeProvider? timeProvider = null, bool enableBackgroundTimer = true, Position? start = null)
    {
        Control = control;
        _ = timeProvider;
        _ = enableBackgroundTimer;
        _ = start;
    }

    public event Action? OnStateChanged;

    public Guid Id { get; } = Guid.NewGuid();

    public TimeControl Control { get; }

    public ChessMode Mode { get; set; }

    public ChessResult? Result => throw new NotImplementedException();

    public bool IsOver => throw new NotImplementedException();

    public DateTimeOffset StartedAtUtc => throw new NotImplementedException();

    public DateTimeOffset? EndedAtUtc => throw new NotImplementedException();

    public TimeSpan? Duration => throw new NotImplementedException();

    public void SetSeat(int seat, string name, Guid? playerId, PieceColor color) => throw new NotImplementedException();

    public PieceColor ColorOf(int seat) => throw new NotImplementedException();

    public int SeatOf(PieceColor color) => throw new NotImplementedException();

    public string GetSeatName(int seat) => throw new NotImplementedException();

    public Guid? GetSeatPlayerId(int seat) => throw new NotImplementedException();

    public string GetPlayerName(PieceColor color) => throw new NotImplementedException();

    public Guid? GetPlayerId(PieceColor color) => throw new NotImplementedException();

    public bool TryMove(PieceColor player, Square from, Square to, PieceType? promotion, out ChessMove? played) =>
        throw new NotImplementedException();

    public ChessSnapshot Snapshot() => throw new NotImplementedException();

    public void Tick() => throw new NotImplementedException();

    public bool TryMarkResultRecorded() => throw new NotImplementedException();

    public void Dispose()
    {
    }

    internal bool RestartCore(bool swapColors) => throw new NotImplementedException();

    partial void TickLifecycle();

    private void RaiseStateChanged() => OnStateChanged?.Invoke();
}
