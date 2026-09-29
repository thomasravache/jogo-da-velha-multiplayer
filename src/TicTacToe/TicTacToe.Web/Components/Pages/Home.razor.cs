using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;

namespace TicTacToe.Web.Components.Pages;

public partial class Home : IDisposable
{
    [Inject] private ShellState Shell { get; set; } = default!;

    private PlayerProfile? _profile;
    private string? ReturningName;
    private string ConnectionId = Guid.NewGuid().ToString();
    private string PlayerName = "";
    private Guid? MatchId;
    private bool IsWaiting;
    private Player MyPlayer;
    private bool _confettiFired;
    private string? CreatedRoomCode;
    private string InputRoomCode = "";
    private string? RoomErrorMessage;
    private bool IsSoloGame;
    private AiDifficulty SelectedDifficulty = AiDifficulty.Hard;

    private SeriesFormat SelectedFormat = SeriesFormat.Single;
    private int SelectedBestOf => SelectedFormat == SeriesFormat.BestOf5 ? 5 : 1;

    private static RematchBar.RematchKind RematchKindFor(GameSession game) =>
        game.Format == SeriesFormat.Single ? RematchBar.RematchKind.Rematch
        : game.IsSeriesOver ? RematchBar.RematchKind.NewSeries : RematchBar.RematchKind.NextRound;
    private static readonly BotTurnRunner BotRunner = new(TimeProvider.System);
    private readonly CancellationTokenSource _botCts = new();

    private void CreateRoom()
    {
        if (string.IsNullOrWhiteSpace(PlayerName)) return;
        RoomErrorMessage = null;
        RememberNickname();
        CreatedRoomCode = Matchmaking.CreatePrivateRoom(ConnectionId, PlayerName.Trim(), _profile?.PlayerId, SelectedBestOf);
    }

    private void JoinRoom()
    {
        if (string.IsNullOrWhiteSpace(PlayerName) || string.IsNullOrWhiteSpace(InputRoomCode)) return;
        RoomErrorMessage = null;
        RememberNickname();
        var matchId = Matchmaking.JoinPrivateRoom(InputRoomCode, ConnectionId, PlayerName.Trim(), _profile?.PlayerId);
        if (matchId != null)
        {
            MatchId = matchId;
            MyPlayer = Player.O;
            EnsureGameExists();
        }
        else
        {
            RoomErrorMessage = "Sala inválida ou já iniciada!";
        }
    }

    private void FindMatch()
    {
        if (string.IsNullOrWhiteSpace(PlayerName)) return;

        IsWaiting = true;
        RememberNickname();
        MatchId = Matchmaking.JoinQueue(ConnectionId, PlayerName.Trim(), _profile?.PlayerId, SelectedBestOf);

        if (MatchId != null)
        {
            MyPlayer = Player.O;
            IsWaiting = false;
            EnsureGameExists();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        // O localStorage só existe depois da primeira renderização interativa.
        _profile = await Identity.LoadAsync();
        if (!string.IsNullOrWhiteSpace(_profile.Nickname))
        {
            ReturningName = _profile.Nickname;
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                PlayerName = _profile.Nickname;
            }
        }

        StateHasChanged();
    }

    private void RememberNickname() => _ = Identity.SaveNicknameAsync(PlayerName).AsTask();

    protected override void OnAfterRender(bool firstRender)
    {
        // Modo imersivo do shell enquanto há partida ativa (esconde a barra inferior no mobile).
        if (MatchId != null && !Shell.Immersive)
        {
            Shell.Set(true, "Partida ativa");
        }
        else if (MatchId == null && Shell.Immersive)
        {
            Shell.Reset();
        }
    }

    protected override void OnInitialized()
    {
        Matchmaking.OnPlayerMatched += OnMatchedReceived;
    }

    private void OnMatchedReceived(string connectionId, Guid matchId)
    {
        if (connectionId == ConnectionId && MatchId == null)
        {
            MatchId = matchId;
            MyPlayer = Player.X;
            IsWaiting = false;
            CreatedRoomCode = null;
            EnsureGameExists();
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private void EnsureGameExists()
    {
        if (MatchId != null && !Games.ContainsKey(MatchId.Value))
        {
            var format = Matchmaking.GetMatchBestOf(MatchId.Value) == 5 ? SeriesFormat.BestOf5 : SeriesFormat.Single;
            var game = new GameSession(format: format) { Mode = Matchmaking.IsPrivateMatch(MatchId.Value) ? GameMode.Private : GameMode.Online };
            Games.TryAdd(MatchId.Value, game);
        }

        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var existingGame))
        {
            existingGame.OnStateChanged -= OnGameStateChanged;
            existingGame.OnStateChanged += OnGameStateChanged;

            var matchPlayers = Matchmaking.GetMatchPlayers(MatchId.Value);
            if (matchPlayers != null)
            {
                MyPlayer = matchPlayers.Value.PlayerX == ConnectionId ? Player.X : Player.O;
            }

            var ids = Matchmaking.GetMatchPlayerIds(MatchId.Value);
            if (ids != null)
            {
                existingGame.SetPlayerId(Player.X, ids.Value.X);
                existingGame.SetPlayerId(Player.O, ids.Value.O);
            }

            var names = Matchmaking.GetMatchPlayerNames(MatchId.Value);
            if (names != null)
            {
                existingGame.SetPlayerName(Player.X, names.Value.PlayerXName);
                existingGame.SetPlayerName(Player.O, names.Value.PlayerOName);
            }
        }
    }

    private async void OnGameStateChanged()
    {
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var g))
        {
            if (g.Winner == Player.None) _confettiFired = false;

            if (g.Winner == MyPlayer && !_confettiFired)
            {
                _confettiFired = true;
                try
                {
                    await JS.InvokeVoidAsync("triggerConfetti");
                }
                catch { }
            }

            await GameResultService.SaveOnceAsync(g);
        }
        await InvokeAsync(StateHasChanged);
    }

    private void StartSoloGame()
    {
        if (string.IsNullOrWhiteSpace(PlayerName)) return;

        RememberNickname();
        IsSoloGame = true;
        MatchId = Guid.NewGuid();
        MyPlayer = Player.X;

        var game = new GameSession(format: SelectedBestOf == 5 ? SeriesFormat.BestOf5 : SeriesFormat.Single) { Mode = GameMode.Solo };
        game.SetPlayerName(Player.X, PlayerName.Trim());
        game.SetPlayerId(Player.X, _profile?.PlayerId);
        game.SetPlayerName(Player.O, AiPlayer.GetBotName(SelectedDifficulty));
        game.OnStateChanged += OnGameStateChanged;
        Games.TryAdd(MatchId.Value, game);
    }

    private async Task MakeMove(int index)
    {
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var game))
        {
            if (game.CurrentTurn == MyPlayer)
            {
                var moved = game.MakeMove(index, MyPlayer);
                if (moved)
                {
                    if (game.Winner != Player.None || game.IsDraw)
                    {
                        await GameResultService.SaveOnceAsync(game);
                    }
                    else if (IsSoloGame && game.CurrentTurn == Player.O)
                    {
                        RunBot(game);
                    }
                }
            }
        }
    }

    // O robô joga depois de um atraso, também ao abrir uma rodada da série; cancelado ao sair da página.
    private void RunBot(GameSession game)
    {
        var token = _botCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await BotRunner.RunAsync(game, Player.O, SelectedDifficulty, TimeSpan.FromMilliseconds(250), token);
                if (game.Winner != Player.None || game.IsDraw)
                {
                    await GameResultService.SaveOnceAsync(game);
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (OperationCanceledException)
            {
                // Página descartada durante o atraso: nada a fazer.
            }
        });
    }

    private void RequestRematch() => WithGame(g => g.RequestRematch(MyPlayer));

    private void AcceptRematch() => WithGame(g => g.AcceptRematch(MyPlayer));

    private void DeclineRematch() => WithGame(g => g.DeclineRematch(MyPlayer));

    private void WithGame(Action<GameSession> action)
    {
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var game))
        {
            action(game);
        }
    }

    private async Task LeaveGame()
    {
        if (MatchId == null || !Games.TryGetValue(MatchId.Value, out var game)) return;

        if (game.Leave(MyPlayer) == LeaveResult.Forfeited)
        {
            await GameResultService.SaveOnceAsync(game);
        }

        ReturnToLobby();
    }

    private void BackToLobby()
    {
        WithGame(g => g.Leave(MyPlayer));
        ReturnToLobby();
    }

    // Volta ao lobby: solta a partida e, se ninguém mais depende dela, remove a sessão.
    private void ReturnToLobby()
    {
        if (MatchId is { } id && Games.TryGetValue(id, out var game))
        {
            game.OnStateChanged -= OnGameStateChanged;
            if (IsSoloGame || (game.HasLeft(Player.X) && game.HasLeft(Player.O)))
            {
                Games.TryRemove(id, out _);
                game.Dispose();
            }
        }

        MatchId = null;
        IsSoloGame = false;
        IsWaiting = false;
        CreatedRoomCode = null;
        _confettiFired = false;
    }

    private void RestartGame()
    {
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var game))
        {
            _confettiFired = false;
            game.Restart();
            if (IsSoloGame && game.CurrentTurn == Player.O && game.Winner == Player.None)
            {
                RunBot(game);
            }
        }
    }

    public void Dispose()
    {
        _botCts.Cancel();
        _botCts.Dispose();
        Shell.Reset();
        Matchmaking.OnPlayerMatched -= OnMatchedReceived;
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var g))
        {
            g.OnStateChanged -= OnGameStateChanged;
        }
        GC.SuppressFinalize(this);
    }
}
