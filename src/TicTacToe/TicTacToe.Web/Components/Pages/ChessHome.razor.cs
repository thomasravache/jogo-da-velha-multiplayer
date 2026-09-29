using Microsoft.AspNetCore.Components;
using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.Chess;
using TicTacToe.Web.Services.PlayerIdentity;

namespace TicTacToe.Web.Components.Pages;

/// <summary>
/// Página /xadrez (SPEC-0056): orquestra lobby, pareamento e arena. Abandono, revanche e solo entram
/// nas SPEC-0058/0060, nos pontos marcados como gancho.
/// </summary>
public partial class ChessHome : IDisposable
{
    private const string QueuePrefix = "xadrez:";

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
        var matchId = Matchmaking.JoinQueue(_connectionId, TrimmedName, _profile?.PlayerId, queueKey: QueueKey);
        if (matchId is { } id)
        {
            EnterMatch(id);
        }
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
        if (matchId is { } id)
        {
            EnterMatch(id);
        }
        else
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

    private void OnMatchedReceived(string connectionId, Guid matchId)
    {
        if (connectionId != _connectionId || _disposed)
        {
            return;
        }

        try
        {
            EnterMatch(matchId);
            _ = InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException)
        {
            // Página descartada durante o pareamento: nada a fazer.
        }
    }

    // Idempotente: chamado pelo evento de pareamento e pelo retorno de JoinQueue/JoinPrivateRoom.
    private void EnterMatch(Guid matchId)
    {
        if (_matchId is not null)
        {
            return;
        }

        // O serviço limpa as preferências logo depois dos handlers: leia tudo AGORA, de forma síncrona.
        if (Matchmaking.GetMatchPlayers(matchId) is not { } players)
        {
            return;
        }

        var first = new MatchSeatInfo(players.PlayerX, ParsePreference(Matchmaking.GetPreference(players.PlayerX)));
        var second = new MatchSeatInfo(players.PlayerO, ParsePreference(Matchmaking.GetPreference(players.PlayerO)));
        var names = Matchmaking.GetMatchPlayerNames(matchId);
        var ids = Matchmaking.GetMatchPlayerIds(matchId);
        var control = ControlOf(Matchmaking.GetMatchQueueKey(matchId));
        var mode = Matchmaking.IsPrivateMatch(matchId) ? ChessMode.Private : ChessMode.Online;
        var coinFlip = CoinFlip;
        var clock = Clock;

        var match = Registry.GetOrCreate(matchId, () =>
        {
            var session = new ChessSession(control, clock) { Mode = mode };
            var firstColor = ColorAssignment.AssignFirst(first.Preference, second.Preference, coinFlip);
            session.SetSeat(0, names?.PlayerXName ?? "Jogador 1", ids?.X, firstColor);
            session.SetSeat(1, names?.PlayerOName ?? "Jogador 2", ids?.O, firstColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
            var created = new ChessMatch(session);
            created.Seats[first.ConnectionId] = 0;
            created.Seats[second.ConnectionId] = 1;
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation("Partida de xadrez {SessionId} criada. Control={Control} Mode={Mode}", session.Id, control.Id, mode);
            }

            return created;
        });

        if (Registry.SeatOf(matchId, _connectionId) is not { } seat)
        {
            return;
        }

        _matchId = matchId;
        _seat = seat;
        Session = match.Session;
        _isWaiting = false;
        _createdRoomCode = null;
        Session.OnStateChanged += OnSessionChanged;
        // Gancho SPEC-0060: presença, abandono e revanche entram aqui.
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

        _matchId = null;
        Session = null;
        _isWaiting = false;
        _createdRoomCode = null;
    }

    public void Dispose()
    {
        _disposed = true;
        Shell.Reset();
        Matchmaking.OnPlayerMatched -= OnMatchedReceived;
        Matchmaking.LeaveQueue(_connectionId);
        Matchmaking.CancelPrivateRoom(_connectionId);
        LeaveMatch();
        GC.SuppressFinalize(this);
    }

    private readonly record struct MatchSeatInfo(string ConnectionId, ColorPreference Preference);
}
