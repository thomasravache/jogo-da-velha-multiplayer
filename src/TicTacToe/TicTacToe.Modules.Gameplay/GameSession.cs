using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TicTacToe.Modules.Gameplay;

public enum Player { None, X, O }

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

    public GameSession(bool enableBackgroundTimer = true, TimeProvider? timeProvider = null)
    {
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

    public void SetPlayerName(Player player, string name) =>
        _playerNames[player] = name;

    public string GetPlayerName(Player player) =>
        _playerNames.TryGetValue(player, out var name) ? name : player.ToString();

    public int GetScore(Player player) =>
        _scores.TryGetValue(player, out var score) ? score : 0;

    public void ResetScores() => _scores.Clear();

    public void Tick()
    {
        lock (_lock)
        {
            if (Winner != Player.None || IsDraw || RemainingSeconds <= 0)
                return;

            RemainingSeconds--;

            if (RemainingSeconds <= 0)
            {
                IsTimedOut = true;
                _endedAt = _time.GetUtcNow();
                _endReason = Gameplay.EndReason.Timeout;
                Winner = CurrentTurn == Player.X ? Player.O : Player.X;
                _scores[Winner] = GetScore(Winner) + 1;
            }
        }

        OnStateChanged?.Invoke();
    }

    public void Restart()
    {
        lock (_lock)
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
            CurrentTurn = Player.X;
            RemainingSeconds = DefaultTurnTimeSeconds;
        }
        OnStateChanged?.Invoke();
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
