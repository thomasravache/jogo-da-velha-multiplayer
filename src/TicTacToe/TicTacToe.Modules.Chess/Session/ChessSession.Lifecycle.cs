namespace TicTacToe.Modules.Chess;

/// <summary>
/// Ciclo de vida da partida (SPEC-0061): desistência, abandono, revanche com troca de cores e presença.
/// Presença e saída são guardadas por assento, para sobreviver à troca de cores na revanche.
/// </summary>
public sealed partial class ChessSession
{
    public const int DisconnectGraceSeconds = 15;

    private static readonly TimeSpan RematchTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DisconnectGrace = TimeSpan.FromSeconds(DisconnectGraceSeconds);

    private readonly bool[] _left = new bool[2];
    private readonly DateTimeOffset?[] _disconnectedAt = [null, null];
    private ChessRematchState _rematchState;
    private PieceColor? _rematchRequestedBy;
    private int _rematchRequesterSeat;
    private DateTimeOffset _rematchRequestedAt;
    private bool _restarting;

    public ChessRematchState RematchState
    {
        get
        {
            lock (_gate)
            {
                return _rematchState;
            }
        }
    }

    /// <summary>Cor de quem pediu a revanche no momento do pedido.</summary>
    public PieceColor? RematchRequestedBy
    {
        get
        {
            lock (_gate)
            {
                return _rematchRequestedBy;
            }
        }
    }

    public bool HasLeft(PieceColor color)
    {
        lock (_gate)
        {
            return _left[SeatOfLocked(color)];
        }
    }

    /// <summary>Único caminho de desistência: o outro lado vence com o motivo dado; falso se a partida já terminou.</summary>
    public bool Forfeit(PieceColor loser, ChessEndReason reason)
    {
        bool forfeited;
        bool flagged;
        lock (_gate)
        {
            flagged = ApplyFlagLocked();
            forfeited = !_restarting && ForfeitLocked(loser, reason);
        }

        if (forfeited || flagged)
        {
            RaiseStateChanged();
        }

        return forfeited;
    }

    public ChessLeaveResult Leave(PieceColor player)
    {
        ChessLeaveResult result;
        bool flagged;
        lock (_gate)
        {
            flagged = ApplyFlagLocked();
            var seat = SeatOfLocked(player);
            if (_restarting || _left[seat])
            {
                result = ChessLeaveResult.Rejected;
            }
            else if (_game.IsOver)
            {
                _left[seat] = true;
                ClearRematchLocked();
                result = ChessLeaveResult.Left;
            }
            else if (_mode == ChessMode.Solo)
            {
                _left[seat] = true;
                _recorded = true; // partida descartada: nada a gravar
                result = ChessLeaveResult.Discarded;
            }
            else
            {
                ForfeitLocked(player, ChessEndReason.Abandon);
                result = ChessLeaveResult.Forfeited;
            }
        }

        if (result != ChessLeaveResult.Rejected || flagged)
        {
            RaiseStateChanged();
        }

        return result;
    }

    public bool RequestRematch(PieceColor player)
    {
        bool accepted;
        bool restart = false;
        bool flagged;
        lock (_gate)
        {
            flagged = ApplyFlagLocked();
            var seat = SeatOfLocked(player);
            var other = 1 - seat;
            if (_restarting || _left[seat] || (!_game.IsOver && _mode != ChessMode.Solo))
            {
                accepted = false;
            }
            else if (_mode == ChessMode.Solo)
            {
                restart = accepted = true;
            }
            else if (_left[other])
            {
                accepted = false;
            }
            else if (_rematchState == ChessRematchState.Requested)
            {
                if (_rematchRequesterSeat == seat)
                {
                    accepted = false;
                }
                else
                {
                    restart = accepted = true; // pedidos simultâneos: aceite automático
                }
            }
            else
            {
                _rematchState = ChessRematchState.Requested;
                _rematchRequestedBy = player;
                _rematchRequesterSeat = seat;
                _rematchRequestedAt = _time.GetUtcNow();
                accepted = true;
            }

            if (restart)
            {
                BeginRestartLocked();
            }
        }

        Finish(restart, accepted || flagged);
        return accepted;
    }

    public bool AcceptRematch(PieceColor player)
    {
        bool accepted;
        bool flagged;
        lock (_gate)
        {
            flagged = ApplyFlagLocked();
            accepted = CanAnswerRematchLocked(player);
            if (accepted)
            {
                BeginRestartLocked();
            }
        }

        Finish(accepted, flagged);
        return accepted;
    }

    public bool DeclineRematch(PieceColor player)
    {
        bool declined;
        lock (_gate)
        {
            declined = CanAnswerRematchLocked(player);
            if (declined)
            {
                _rematchState = ChessRematchState.Declined;
            }
        }

        if (declined)
        {
            RaiseStateChanged();
        }

        return declined;
    }

    /// <summary>Informa queda (false) ou retorno (true) do jogador; ignorado em solo, partida encerrada e jogador ausente.</summary>
    public void SetConnection(PieceColor player, bool connected)
    {
        lock (_gate)
        {
            var seat = SeatOfLocked(player);
            if (_mode == ChessMode.Solo || _game.IsOver || _left[seat] || _restarting)
            {
                return;
            }

            if (connected)
            {
                if (_disconnectedAt[seat] is null)
                {
                    return;
                }

                _disconnectedAt[seat] = null;
            }
            else
            {
                if (_disconnectedAt[seat] is not null)
                {
                    return;
                }

                _disconnectedAt[seat] = _time.GetUtcNow();
            }
        }

        RaiseStateChanged();
    }

    /// <summary>Segundos que faltam para o W.O. do jogador desconectado; nulo se conectado ou partida encerrada.</summary>
    public int? DisconnectSecondsLeft(PieceColor player)
    {
        lock (_gate)
        {
            if (_game.IsOver || _disconnectedAt[SeatOfLocked(player)] is not { } since)
            {
                return null;
            }

            var elapsed = (int)Math.Floor((_time.GetUtcNow() - since).TotalSeconds);
            return Math.Max(0, DisconnectGraceSeconds - elapsed);
        }
    }

    partial void TickLifecycle()
    {
        var changed = false;
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            if (_rematchState == ChessRematchState.Requested && now - _rematchRequestedAt >= RematchTimeout)
            {
                _rematchState = ChessRematchState.Expired;
                changed = true;
            }

            if (!_game.IsOver && _mode != ChessMode.Solo && !_restarting)
            {
                var loserSeat = -1;
                DateTimeOffset? earliest = null;
                for (var seat = 0; seat < 2; seat++)
                {
                    if (_disconnectedAt[seat] is { } since && now - since >= DisconnectGrace && (earliest is null || since < earliest))
                    {
                        earliest = since;
                        loserSeat = seat;
                    }
                }

                if (loserSeat >= 0)
                {
                    ForfeitLocked(ColorOfLocked(loserSeat), ChessEndReason.Disconnect);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            RaiseStateChanged();
        }
    }

    // Chamado com o lock adquirido. Só desistência/abandono/queda/tempo; abandono e queda só valem em partida humana.
    private bool ForfeitLocked(PieceColor loser, ChessEndReason reason)
    {
        var solo = _mode == ChessMode.Solo;
        var valid = reason switch
        {
            ChessEndReason.Resignation or ChessEndReason.Timeout => true,
            ChessEndReason.Abandon or ChessEndReason.Disconnect => !solo,
            _ => false,
        };
        if (!valid || _game.IsOver)
        {
            return false;
        }

        var winner = Opposite(loser);
        _game.End(new ChessResult(winner == PieceColor.White ? ChessOutcome.WhiteWins : ChessOutcome.BlackWins, reason));
        EndLocked();
        if (!solo)
        {
            _left[SeatOfLocked(loser)] = true;
        }

        Array.Clear(_disconnectedAt);
        ClearRematchLocked();
        return true;
    }

    private void ClearRematchLocked()
    {
        _rematchState = ChessRematchState.None;
        _rematchRequestedBy = null;
    }

    // Só quem recebeu o pedido responde, com a partida encerrada e os dois ainda na partida.
    private bool CanAnswerRematchLocked(PieceColor player)
    {
        var seat = SeatOfLocked(player);
        return !_restarting
            && _rematchState == ChessRematchState.Requested
            && _rematchRequesterSeat != seat
            && _game.IsOver
            && !_left[seat]
            && !_left[_rematchRequesterSeat];
    }

    // Com o lock adquirido: limpa o ciclo de vida da partida que acabou; RestartCore roda fora do lock (Finish).
    private void BeginRestartLocked()
    {
        _restarting = true;
        Array.Clear(_left);
        Array.Clear(_disconnectedAt);
        ClearRematchLocked();
    }

    private void Finish(bool restart, bool raise)
    {
        if (restart)
        {
            try
            {
                RestartCore(swapColors: true);
            }
            finally
            {
                lock (_gate)
                {
                    _restarting = false;
                }
            }
        }
        else if (raise)
        {
            RaiseStateChanged();
        }
    }
}
