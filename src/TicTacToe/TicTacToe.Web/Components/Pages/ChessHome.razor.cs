using Microsoft.AspNetCore.Components;
using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.Chess;
using TicTacToe.Web.Services.PlayerIdentity;

namespace TicTacToe.Web.Components.Pages;

/// <summary>
/// Página /xadrez (SPEC-0056): orquestra lobby, pareamento e arena. O treino solo contra o robô é da SPEC-0058;
/// abandono e revanche entre humanos e presença entram na SPEC-0060, nos pontos marcados como gancho.
/// </summary>
public partial class ChessHome : IDisposable
{
    private const string QueuePrefix = "xadrez:";
    private const int BotSeat = 1;

    private static readonly TimeSpan BotDelay = TimeSpan.FromMilliseconds(600);

    private readonly Lock _gate = new();
    private readonly string _connectionId = Guid.NewGuid().ToString();
    private PlayerProfile? _profile;
    private string? _returningName;
    private string _playerName = "";
    private TimeControl _control = TimeControl.Blitz;
    private ColorPreference _color = ColorPreference.Random;
    private bool _isWaiting;
    private string? _createdRoomCode;
    private string _inputRoomCode = "";
    private string? _roomError;
    private Guid? _matchId;
    private int _seat;
    private bool _disposed;
    private ChessBotLevel _level = ChessBotLevel.Easy;
    private bool _solo;
    private IChessBot? _bot;
    private CancellationTokenSource? _botCts;
    private int _botBusy;

    [Inject] private MatchmakingService Matchmaking { get; set; } = default!;

    [Inject] private ChessMatchRegistry Registry { get; set; } = default!;

    [Inject] private ChessResultRecorder Recorder { get; set; } = default!;

    [Inject] private PlayerIdentityService Identity { get; set; } = default!;

    [Inject] private ShellState Shell { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [Inject] private ILogger<ChessHome> Logger { get; set; } = default!;

    private ChessSession? Session { get; set; }

    // Serviços opcionais: relógio e cara-ou-coroa injetáveis (testes).
    private TimeProvider Clock => Services.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;

    // Fábrica do robô opcional (testes injetam robôs roteirizados).
    private Func<ChessBotLevel, IChessBot> BotFactory =>
        Services.GetService(typeof(Func<ChessBotLevel, IChessBot>)) as Func<ChessBotLevel, IChessBot> ?? (level => ChessBots.Create(level));

    private Func<bool> CoinFlip => Services.GetService(typeof(Func<bool>)) as Func<bool> ?? (() => Random.Shared.Next(2) == 0);

    protected override void OnInitialized() => Matchmaking.OnPlayerMatched += OnMatchedReceived;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // O armazenamento do navegador só existe depois da primeira renderização interativa.
        _profile = await Identity.LoadAsync();
        if (!string.IsNullOrWhiteSpace(_profile.Nickname))
        {
            _returningName = _profile.Nickname;
            if (string.IsNullOrWhiteSpace(_playerName))
            {
                _playerName = _profile.Nickname;
            }
        }

        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // Modo imersivo do shell enquanto há partida ativa.
        if (Session is not null && !Shell.Immersive)
        {
            Shell.Set(true, "Partida de xadrez");
        }
        else if (Session is null && Shell.Immersive)
        {
            Shell.Reset();
        }
    }

    private string TrimmedName
    {
        get
        {
            var name = _playerName.Trim();
            return name[..Math.Min(PlayerIdentityService.MaxNicknameLength, name.Length)];
        }
    }

    private string QueueKey => QueuePrefix + _control.Id;

    private bool CanStart => !string.IsNullOrWhiteSpace(_playerName);

    private void RememberNickname() => _ = Identity.SaveNicknameAsync(_playerName).AsTask();

    // A preferência precisa estar registrada antes de entrar: o pareamento dispara dentro de JoinQueue/JoinPrivateRoom.
    private void Prepare()
    {
        // Fila e sala são exclusivas: cancela o que estiver pendente antes de uma nova ação.
        Matchmaking.LeaveQueue(_connectionId);
        Matchmaking.CancelPrivateRoom(_connectionId);
        _isWaiting = false;
        _createdRoomCode = null;
        _roomError = null;
        RememberNickname();
        Matchmaking.SetMatchPreference(_connectionId, _color.ToString());
    }

    private void FindMatch()
    {
        if (!CanStart)
        {
            return;
        }

        Prepare();
        _isWaiting = true;
        Matchmaking.JoinQueue(_connectionId, TrimmedName, _profile?.PlayerId, queueKey: QueueKey); // o pareamento chega por OnPlayerMatched
    }

    private void CreateRoom()
    {
        if (!CanStart)
        {
            return;
        }

        Prepare();
        _createdRoomCode = Matchmaking.CreatePrivateRoom(_connectionId, TrimmedName, _profile?.PlayerId, queueKey: QueueKey);
    }

    private void JoinRoom()
    {
        if (!CanStart || string.IsNullOrWhiteSpace(_inputRoomCode))
        {
            return;
        }

        Prepare();
        var matchId = Matchmaking.JoinPrivateRoom(_inputRoomCode, _connectionId, TrimmedName, _profile?.PlayerId, game: "xadrez");
        if (matchId is null)
        {
            Matchmaking.LeaveQueue(_connectionId); // descarta a preferência da tentativa inválida
            _roomError = "Sala inválida ou já iniciada!";
        }
    }

    private void CancelSearch()
    {
        Matchmaking.LeaveQueue(_connectionId);
        Matchmaking.CancelPrivateRoom(_connectionId);
        _isWaiting = false;
        _createdRoomCode = null;
    }

    private void StartSolo()
    {
        if (!CanStart)
        {
            return;
        }

        // Fila e sala são exclusivas do solo: cancela o que estiver pendente (sem registrar preferência de cor).
        Matchmaking.LeaveQueue(_connectionId);
        Matchmaking.CancelPrivateRoom(_connectionId);
        _isWaiting = false;
        _createdRoomCode = null;
        _roomError = null;
        RememberNickname();

        lock (_gate)
        {
            if (_disposed || _matchId is not null)
            {
                return;
            }

            var clock = Clock;
            var control = _control;
            var level = _level;
            var name = TrimmedName;
            var playerId = _profile?.PlayerId;
            // O humano é o "primeiro" jogador; o robô nunca tem preferência (Aleatória sorteia uma vez).
            var humanColor = ColorAssignment.AssignFirst(_color, ColorPreference.Random, CoinFlip);
            var matchId = Guid.NewGuid();
            var match = Registry.GetOrCreate(matchId, () =>
            {
                var session = new ChessSession(control, clock) { Mode = ChessMode.Solo };
                session.SetSeat(0, name, playerId, humanColor);
                session.SetSeat(BotSeat, ChessBots.NameOf(level), null, humanColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
                return new ChessMatch(session);
            });

            match.Seats[_connectionId] = 0;
            _matchId = matchId;
            _seat = 0;
            Session = match.Session;
            _solo = true;
            _bot = BotFactory(level);
            _botCts = new CancellationTokenSource();
            Session.OnStateChanged += OnSessionChanged;
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation("Partida solo de xadrez {SessionId} iniciada. Level={Level} Control={Control}", match.Session.Id, level, control.Id);
            }
        }

        ScheduleBot(); // o robô abre a partida quando o humano joga de pretas
    }

    // Aciona o robô sempre que for a vez dele; a cor do assento é relida a cada estado (muda na revanche).
    private void ScheduleBot()
    {
        ChessSession session;
        IChessBot bot;
        CancellationToken token;
        lock (_gate)
        {
            if (_disposed || !_solo || Session is null || _bot is null || _botCts is null)
            {
                return;
            }

            session = Session;
            bot = _bot;
            token = _botCts.Token;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        var snapshot = session.Snapshot();
        var botColor = session.ColorOf(BotSeat);
        if (snapshot.Result is not null || snapshot.SideToMove != botColor)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _botBusy, 1, 0) != 0)
        {
            return; // já há uma jogada agendada; ao terminar ela reavalia o estado
        }

        var signature = (snapshot.Moves.Count, botColor);
        var runner = new ChessBotTurnRunner(Clock);
        _ = Task.Run(async () =>
        {
            try
            {
                await runner.RunAsync(session, BotSeat, bot, BotDelay, token);
            }
            catch (OperationCanceledException)
            {
                // Página descartada ou partida abandonada durante o atraso: nada a fazer.
            }
            catch (Exception ex)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                {
                    Logger.LogError(ex, "Falha na jogada do robô da partida de xadrez {SessionId}.", session.Id);
                }
            }
            finally
            {
                Volatile.Write(ref _botBusy, 0);
            }

            // Se o estado mudou durante a espera (lance, revanche com troca de cores), reavalia de quem é a vez.
            if (!token.IsCancellationRequested && (session.Snapshot().Moves.Count, session.ColorOf(BotSeat)) != signature)
            {
                ScheduleBot();
            }
        });
    }

    private async Task RematchSolo()
    {
        var session = Session;
        if (session is null || !_solo)
        {
            return;
        }

        await Recorder.SaveOnceAsync(session); // grava antes da revanche, que zera a partida
        session.RequestRematch(session.ColorOf(_seat));
    }

    // Abandonar partida solo em andamento descarta a sessão sem gravar.
    private void AbandonSolo()
    {
        lock (_gate)
        {
            if (Session is not { } session || !_solo)
            {
                return;
            }

            // Sem o handler, o aviso do Leave não reagenda o robô.
            session.OnStateChanged -= OnSessionChanged;
            if (session.Leave(session.ColorOf(_seat)) == ChessLeaveResult.Rejected)
            {
                session.OnStateChanged += OnSessionChanged;
                return;
            }

            LeaveMatchCore();
        }
    }

    private void OnMatchedReceived(string connectionId, Guid matchId)
    {
        if (connectionId != _connectionId || _disposed)
        {
            return;
        }

        try
        {
            // O serviço limpa as preferências logo depois dos handlers: leia tudo AGORA, de forma síncrona.
            if (ReadMatch(matchId) is not { } snapshot)
            {
                return;
            }

            _ = InvokeAsync(() =>
            {
                EnterMatch(snapshot);
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            // Não propaga ao JoinQueue do oponente.
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(ex, "Falha ao tratar o pareamento de xadrez {MatchId}.", matchId);
            }
        }
    }

    private MatchSnapshot? ReadMatch(Guid matchId)
    {
        if (Matchmaking.GetMatchPlayers(matchId) is not { } players)
        {
            return null;
        }

        var names = Matchmaking.GetMatchPlayerNames(matchId);
        var ids = Matchmaking.GetMatchPlayerIds(matchId);
        return new MatchSnapshot(
            matchId,
            new MatchSeatInfo(players.PlayerX, ParsePreference(Matchmaking.GetPreference(players.PlayerX)), names?.PlayerXName ?? "Jogador 1", ids?.X),
            new MatchSeatInfo(players.PlayerO, ParsePreference(Matchmaking.GetPreference(players.PlayerO)), names?.PlayerOName ?? "Jogador 2", ids?.O),
            ControlOf(Matchmaking.GetMatchQueueKey(matchId)),
            Matchmaking.IsPrivateMatch(matchId) ? ChessMode.Private : ChessMode.Online);
    }

    // Roda no dispatcher do circuito; o lock serializa com Dispose/LeaveMatch para não registrar assento em página morta.
    private void EnterMatch(MatchSnapshot m)
    {
        lock (_gate)
        {
            if (_disposed || _matchId is not null)
            {
                return;
            }

            var coinFlip = CoinFlip;
            var clock = Clock;
            var match = Registry.GetOrCreate(m.MatchId, () =>
            {
                var session = new ChessSession(m.Control, clock) { Mode = m.Mode };
                var firstColor = ColorAssignment.AssignFirst(m.First.Preference, m.Second.Preference, coinFlip);
                session.SetSeat(0, m.First.Name, m.First.PlayerId, firstColor);
                session.SetSeat(1, m.Second.Name, m.Second.PlayerId, firstColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation("Partida de xadrez {SessionId} criada. Control={Control} Mode={Mode}", session.Id, m.Control.Id, m.Mode);
                }

                return new ChessMatch(session);
            });

            // Cada página registra só o próprio assento: quem foi descartado antes não segura a sessão.
            var seat = m.First.ConnectionId == _connectionId ? 0 : 1;
            match.Seats[_connectionId] = seat;
            _matchId = m.MatchId;
            _seat = seat;
            Session = match.Session;
            _isWaiting = false;
            _createdRoomCode = null;
            Session.OnStateChanged += OnSessionChanged;
            // Gancho SPEC-0060: presença, abandono e revanche entram aqui.
        }
    }

    private static ColorPreference ParsePreference(string? raw) =>
        Enum.TryParse<ColorPreference>(raw, out var preference) ? preference : ColorPreference.Random;

    private static TimeControl ControlOf(string queueKey) =>
        queueKey.StartsWith(QueuePrefix, StringComparison.Ordinal) && TimeControl.FromId(queueKey[QueuePrefix.Length..]) is { } control
            ? control
            : TimeControl.Blitz;

    // A sessão avisa de qualquer thread (lance do oponente, bandeira caída): grava o resultado e redesenha.
    private void OnSessionChanged()
    {
        var session = Session;
        if (_disposed || session is null)
        {
            return;
        }

        if (_solo)
        {
            ScheduleBot();
        }

        try
        {
            _ = InvokeAsync(async () =>
            {
                if (_disposed)
                {
                    return;
                }

                await Recorder.SaveOnceAsync(session); // antes de qualquer revanche; grava uma vez por partida
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException)
        {
            // Página descartada: nada a redesenhar.
        }
    }

    private void BackToLobby() => LeaveMatch();

    // Solta a partida; a última pessoa a sair remove a sessão do registro (e para o relógio).
    private void LeaveMatch()
    {
        lock (_gate)
        {
            LeaveMatchCore();
        }
    }

    private void LeaveMatchCore()
    {
        if (_matchId is { } id)
        {
            if (Session is not null)
            {
                Session.OnStateChanged -= OnSessionChanged;
            }

            if (Registry.TryGet(id, out var match))
            {
                match.Seats.TryRemove(_connectionId, out _);
                if (match.Seats.IsEmpty)
                {
                    Registry.Remove(id);
                }
            }

            // Gancho SPEC-0060: abandono por saída da página entra aqui.
        }

        // Sem lance do robô depois do descarte. O token só é cancelado (não há temporizador a liberar no CTS).
        _botCts?.Cancel();
        _botCts = null;
        _bot = null;
        _solo = false;
        _matchId = null;
        Session = null;
        _isWaiting = false;
        _createdRoomCode = null;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            LeaveMatchCore();
        }

        Shell.Reset();
        Matchmaking.OnPlayerMatched -= OnMatchedReceived;
        Matchmaking.LeaveQueue(_connectionId);
        Matchmaking.CancelPrivateRoom(_connectionId);
        GC.SuppressFinalize(this);
    }

    private readonly record struct MatchSeatInfo(string ConnectionId, ColorPreference Preference, string Name, Guid? PlayerId);

    private sealed record MatchSnapshot(Guid MatchId, MatchSeatInfo First, MatchSeatInfo Second, TimeControl Control, ChessMode Mode);
}
