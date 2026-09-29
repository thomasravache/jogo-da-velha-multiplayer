using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TicTacToe.Modules.Gameplay;

public enum Player { None, X, O }

public enum LeaveResult { Forfeited, Discarded, Left, Rejected }

public enum RematchState { None, Requested, Declined, Expired }

#pragma warning disable CA1720 // nome definido pelo contrato da SPEC-0040
public enum SeriesFormat { Single = 0, BestOf5 = 1 }
#pragma warning restore CA1720

public class GameSession : IDisposable
{
    public const int DefaultTurnTimeSeconds = 15;

    public Guid Id { get; } = Guid.NewGuid();

    private readonly Dictionary<Player, string> _playerNames = new();
    private readonly Dictionary<Player, int> _scores = new();
    private readonly object _lock = new();
    private readonly Timer? _timer;

    public int RemainingSeconds { get; private set; } = DefaultTurnTimeSeconds;
    public bool IsTimedOut { get; private set; }

    public Player[] Board { get; } = new Player[9];
    public Player CurrentTurn { get; private set; } = Player.X;
    public Player Winner { get; private set; } = Player.None;
    public bool IsDraw => Winner == Player.None && Array.TrueForAll(Board, p => p != Player.None);

    public event Action? OnStateChanged;

    public GameSession(bool enableBackgroundTimer = true, TimeProvider? timeProvider = null, SeriesFormat format = SeriesFormat.Single)
    {
        _format = format;
        _seriesId = format == SeriesFormat.BestOf5 ? Guid.NewGuid() : null;
        _time = timeProvider ?? TimeProvider.System;
        _startedAt = _time.GetUtcNow();
        if (enableBackgroundTimer)
        {
            _timer = new Timer(_ => Tick(), null, 1000, 1000);
        }
    }

    private readonly TimeProvider _time;
    private GameMode _mode = GameMode.Online;
    private DateTimeOffset _startedAt;
    private DateTimeOffset? _endedAt;
    private int _moveCount;
    private EndReason? _endReason;

    /// <summary>Modo em que a partida foi criada (online, sala privada ou solo).</summary>
    public GameMode Mode
    {
        get { lock (_lock) { return _mode; } }
        set { lock (_lock) { _mode = value; } }
    }

    /// <summary>Instante (UTC) em que a rodada começou: criação ou último <see cref="Restart"/>.</summary>
    public DateTimeOffset StartedAtUtc
    {
        get { lock (_lock) { return _startedAt; } }
    }

    /// <summary>Instante (UTC) do fim da rodada (vitória, empate ou estouro do tempo); nulo em andamento.</summary>
    public DateTimeOffset? EndedAtUtc
    {
        get { lock (_lock) { return _endedAt; } }
    }

    public TimeSpan? Duration
    {
        get { lock (_lock) { return _endedAt - _startedAt; } }
    }

    /// <summary>Jogadas válidas da rodada atual.</summary>
    public int MoveCount
    {
        get { lock (_lock) { return _moveCount; } }
    }

    public EndReason? EndReason
    {
        get { lock (_lock) { return _endReason; } }
    }

    /// <summary>Nove caracteres (casas 0..8): X, O ou -.</summary>
    public string FinalBoard
    {
        get
        {
            lock (_lock)
            {
                return string.Concat(Board.Select(p => p == Player.X ? 'X' : p == Player.O ? 'O' : '-'));
            }
        }
    }

    private bool _resultRecorded;
    private IReadOnlyList<int>? _winningLine;

    /// <summary>Três índices (0–8, crescentes) da linha que deu a vitória por jogada; nulo em outros casos.</summary>
    public IReadOnlyList<int>? WinningLine
    {
        get
        {
            lock (_lock)
            {
                return _winningLine;
            }
        }
    }

    /// <summary>Marca atomicamente que o resultado da rodada foi gravado; verdadeiro só no primeiro chamador.</summary>
    public bool TryMarkResultRecorded()
    {
        lock (_lock)
        {
            if (_resultRecorded) return false;
            _resultRecorded = true;
            return true;
        }
    }

    private readonly Dictionary<Player, Guid> _playerIds = new();

    /// <summary>Identidade anônima do jogador (nula para o robô e para quem não tem identidade).</summary>
    public void SetPlayerId(Player player, Guid? id)
    {
        lock (_lock)
        {
            if (id is { } value)
            {
                _playerIds[player] = value;
            }
            else
            {
                _playerIds.Remove(player);
            }
        }
    }

    public Guid? GetPlayerId(Player player)
    {
        lock (_lock)
        {
            return _playerIds.TryGetValue(player, out var id) ? id : null;
        }
    }

    // Série melhor de 5 (SPEC-0040). Scaffold: as regras da série ainda não estão implementadas.
    private readonly SeriesFormat _format;
    private Guid? _seriesId;

    public SeriesFormat Format => _format;
    public Guid? SeriesId => _seriesId;
    private int RoundsDecided { get; set; }
    private bool SeriesOver { get; set; }
    private Player _seriesWinner = Player.None;
    private Player _roundStarter = Player.X;

    public int RoundNumber => RoundsDecided + 1;
    public int SeriesTarget => _format == SeriesFormat.BestOf5 ? 3 : 1;
    public bool IsSeriesOver => SeriesOver;
    public Player SeriesWinner => _seriesWinner;
    public Player RoundStarter => _roundStarter;
    public bool IsMatchPoint(Player player) => !SeriesOver && _format == SeriesFormat.BestOf5 && GetScore(player) == SeriesTarget - 1;

    // Abandono e revanche com aceite (SPEC-0041): estado compartilhado pelos dois circuitos, sob o lock da sessão.
    private static readonly TimeSpan RematchTimeout = TimeSpan.FromSeconds(30);
    private readonly HashSet<Player> _left = [];
    private DateTimeOffset _rematchRequestedAt;

    public RematchState RematchState { get; private set; }
    public Player? RematchRequestedBy { get; private set; }

    public bool HasLeft(Player player)
    {
        lock (_lock) { return _left.Contains(player); }
    }

    private bool RoundEnded => Winner != Player.None || IsDraw;

    private static Player Other(Player player) => player == Player.X ? Player.O : Player.X;

    private void ClearRematch()
    {
        RematchState = RematchState.None;
        RematchRequestedBy = null;
    }

    public LeaveResult Leave(Player player)
    {
        LeaveResult result;
        lock (_lock)
        {
            if (player == Player.None || !_left.Add(player)) return LeaveResult.Rejected;

            if (RoundEnded)
            {
                ClearRematch();
                result = LeaveResult.Left;
            }
            else if (_mode == GameMode.Solo)
            {
                _resultRecorded = true; // partida descartada: nada a gravar
                result = LeaveResult.Discarded;
            }
            else
            {
                var opponent = Other(player);
                Winner = opponent;
                _endedAt = _time.GetUtcNow();
                _endReason = Gameplay.EndReason.Abandon;
                _scores[opponent] = GetScore(opponent) + 1;
                RegisterRoundWon(opponent);
                if (_format == SeriesFormat.BestOf5)
                {
                    SeriesOver = true;
                    _seriesWinner = opponent;
                }

                result = LeaveResult.Forfeited;
            }
        }

        OnStateChanged?.Invoke();
        return result;
    }

    public bool RequestRematch(Player player)
    {
        lock (_lock)
        {
            if (player == Player.None || _left.Contains(player) || !RoundEnded) return false;
            if (_format == SeriesFormat.BestOf5 && !SeriesOver) return false; // dentro da série a próxima rodada é imediata (Restart)

            if (_mode == GameMode.Solo)
            {
                RestartCore();
            }
            else if (_left.Contains(Other(player)))
            {
                return false;
            }
            else if (RematchState == RematchState.Requested)
            {
                if (RematchRequestedBy == player) return false;
                RestartCore(); // pedidos simultâneos: aceite automático
            }
            else
            {
                RematchState = RematchState.Requested;
                RematchRequestedBy = player;
                _rematchRequestedAt = _time.GetUtcNow();
            }
        }

        OnStateChanged?.Invoke();
        return true;
    }

    public bool AcceptRematch(Player player)
    {
        lock (_lock)
        {
            if (!CanAnswerRematch(player)) return false;
            RestartCore();
        }

        OnStateChanged?.Invoke();
        return true;
    }

    public bool DeclineRematch(Player player)
    {
        lock (_lock)
        {
            if (!CanAnswerRematch(player)) return false;
            RematchState = RematchState.Declined;
        }

        OnStateChanged?.Invoke();
        return true;
    }

    // Chamado com o lock adquirido: só quem recebeu o pedido responde, com os dois ainda na partida.
    private bool CanAnswerRematch(Player player) =>
        RematchState == RematchState.Requested && RematchRequestedBy is { } requester && requester != player
        && player != Player.None && RoundEnded && !_left.Contains(player) && !_left.Contains(requester);

    public void SetPlayerName(Player player, string name) =>
        _playerNames[player] = name;

    public string GetPlayerName(Player player) =>
        _playerNames.TryGetValue(player, out var name) ? name : player.ToString();

    public int GetScore(Player player) =>
        _scores.TryGetValue(player, out var score) ? score : 0;

    public void ResetScores()
    {
        lock (_lock)
        {
            _scores.Clear();
            RoundsDecided = 0;
            SeriesOver = false;
            _seriesWinner = Player.None;
            _roundStarter = Player.X;
        }
    }

    public void Tick()
    {
        var changed = false;
        lock (_lock)
        {
            if (RematchState == RematchState.Requested && _time.GetUtcNow() - _rematchRequestedAt >= RematchTimeout)
            {
                RematchState = RematchState.Expired;
                changed = true;
            }

            if (Winner != Player.None || IsDraw || RemainingSeconds <= 0 || _left.Count > 0)
            {
                if (changed) OnStateChanged?.Invoke();
                return;
            }

            RemainingSeconds--;

            if (RemainingSeconds <= 0)
            {
                IsTimedOut = true;
                _endedAt = _time.GetUtcNow();
                _endReason = Gameplay.EndReason.Timeout;
                Winner = CurrentTurn == Player.X ? Player.O : Player.X;
                _scores[Winner] = GetScore(Winner) + 1;
                RegisterRoundWon(Winner);
            }
        }

        OnStateChanged?.Invoke();
    }

    public void Restart()
    {
        lock (_lock)
        {
            if (!RestartCore()) return;
        }

        OnStateChanged?.Invoke();
    }

    // Chamado com o lock adquirido; falso quando não há o que reiniciar (rodada em andamento numa série).
    private bool RestartCore()
    {
        if (_format == SeriesFormat.BestOf5)
        {
            if (SeriesOver)
            {
                // Série encerrada: nova série com placar zerado, novo id e X abrindo.
                _scores.Clear();
                RoundsDecided = 0;
                SeriesOver = false;
                _seriesWinner = Player.None;
                _seriesId = Guid.NewGuid();
            }
            else if (Winner == Player.None && !IsDraw)
            {
                return false;
            }

            _roundStarter = RoundsDecided % 2 == 0 ? Player.X : Player.O;
        }

        ResetRound();
        return true;
    }

    // Chamado com o lock adquirido: reinicia o tabuleiro da rodada.
    private void ResetRound()
    {
        Array.Clear(Board, 0, Board.Length);
        _resultRecorded = false;
        _winningLine = null;
        _startedAt = _time.GetUtcNow();
        _endedAt = null;
        _endReason = null;
        _moveCount = 0;
        Winner = Player.None;
        IsTimedOut = false;
        CurrentTurn = _format == SeriesFormat.BestOf5 ? _roundStarter : Player.X;
        RemainingSeconds = DefaultTurnTimeSeconds;
        ClearRematch();
    }

    // Chamado com o lock adquirido, após somar o ponto da rodada.
    private void RegisterRoundWon(Player winner)
    {
        if (_format != SeriesFormat.BestOf5) return;

        RoundsDecided++;
        _roundStarter = RoundsDecided % 2 == 0 ? Player.X : Player.O;
        if (GetScore(winner) >= SeriesTarget)
        {
            SeriesOver = true;
            _seriesWinner = winner;
        }
    }

    public bool MakeMove(int index, Player player)
    {
        lock (_lock)
        {
            if (Winner != Player.None || index < 0 || index > 8 || Board[index] != Player.None || CurrentTurn != player)
                return false;

            Board[index] = player;
            _moveCount++;

            var line = FindWinningLine(player);
            if (line is not null)
            {
                Winner = player;
                _winningLine = Array.AsReadOnly((int[])line.Clone());
                _scores[player] = GetScore(player) + 1;
                _endedAt = _time.GetUtcNow();
                _endReason = Gameplay.EndReason.Line;
                RegisterRoundWon(player);
            }
            else
            {
                CurrentTurn = player == Player.X ? Player.O : Player.X;
                RemainingSeconds = DefaultTurnTimeSeconds;

                if (Array.TrueForAll(Board, p => p != Player.None))
                {
                    _endedAt = _time.GetUtcNow();
                    _endReason = Gameplay.EndReason.Draw;
                }
            }
        }
        OnStateChanged?.Invoke();
        return true;
    }

    private static readonly int[][] WinLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // linhas
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // colunas
        [0, 4, 8], [2, 4, 6],            // diagonais
    ];

    private int[]? FindWinningLine(Player player)
    {
        foreach (var line in WinLines)
        {
            if (Board[line[0]] == player && Board[line[1]] == player && Board[line[2]] == player)
                return line;
        }

        return null;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
