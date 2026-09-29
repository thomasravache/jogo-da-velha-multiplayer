using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;

namespace TicTacToe.Web.Components.Pages;

public partial class Home : IDisposable
{
    private string ConnectionId = Guid.NewGuid().ToString();
    private string PlayerName = "";
    private Guid? MatchId;
    private bool IsWaiting;
    private Player MyPlayer;
    private System.Threading.Timer? _pollTimer;
    private bool _confettiFired;
    private string? CreatedRoomCode;
    private string InputRoomCode = "";
    private string? RoomErrorMessage;
    private bool IsSoloGame;
    private AiDifficulty SelectedDifficulty = AiDifficulty.Hard;

    private void CreateRoom()
    {
        if (string.IsNullOrWhiteSpace(PlayerName)) return;
        RoomErrorMessage = null;
        CreatedRoomCode = Matchmaking.CreatePrivateRoom(ConnectionId, PlayerName.Trim());
        _pollTimer = new System.Threading.Timer(CheckMatchStatus, null, 500, 500);
    }

    private void JoinRoom()
    {
        if (string.IsNullOrWhiteSpace(PlayerName) || string.IsNullOrWhiteSpace(InputRoomCode)) return;
        RoomErrorMessage = null;
        var matchId = Matchmaking.JoinPrivateRoom(InputRoomCode, ConnectionId, PlayerName.Trim());
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
        MatchId = Matchmaking.JoinQueue(ConnectionId, PlayerName.Trim());

        if (MatchId != null)
        {
            MyPlayer = Player.O;
            IsWaiting = false;
            EnsureGameExists();
        }
        else
        {
            _pollTimer = new System.Threading.Timer(CheckMatchStatus, null, 500, 500);
        }
    }

    private void CheckMatchStatus(object? state)
    {
        if (Matchmaking.ActiveMatches.TryGetValue(ConnectionId, out var matchId))
        {
            MatchId = matchId;
            MyPlayer = Player.X;
            IsWaiting = false;
            EnsureGameExists();
            _pollTimer?.Dispose();
            InvokeAsync(StateHasChanged);
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
            var game = new GameSession();
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

            var names = Matchmaking.GetMatchPlayerNames(MatchId.Value);
            if (names != null)
            {
                existingGame.SetPlayerName(Player.X, names.Value.PlayerXName);
                existingGame.SetPlayerName(Player.O, names.Value.PlayerOName);
            }
        }

        _pollTimer = new System.Threading.Timer(async _ => await OnPollTick(), null, 1000, 1000);
    }

    private async void OnGameStateChanged()
    {
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var g))
        {
            if (g.Winner == MyPlayer && !_confettiFired)
            {
                _confettiFired = true;
                try
                {
                    await JS.InvokeVoidAsync("triggerConfetti");
                }
                catch { }
            }
        }
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnPollTick()
    {
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var g))
        {
            if (g.Winner == MyPlayer && !_confettiFired)
            {
                _confettiFired = true;
                try
                {
                    await JS.InvokeVoidAsync("triggerConfetti");
                }
                catch { }
            }
        }
        await InvokeAsync(StateHasChanged);
    }

    private void StartSoloGame()
    {
        if (string.IsNullOrWhiteSpace(PlayerName)) return;

        IsSoloGame = true;
        MatchId = Guid.NewGuid();
        MyPlayer = Player.X;

        var game = new GameSession();
        game.SetPlayerName(Player.X, PlayerName.Trim());
        game.SetPlayerName(Player.O, AiPlayer.GetBotName(SelectedDifficulty));
        game.OnStateChanged += OnGameStateChanged;
        Games.TryAdd(MatchId.Value, game);

        _pollTimer = new System.Threading.Timer(async _ => await OnPollTick(), null, 1000, 1000);
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
                        await GameResultService.SaveResultAsync(game);
                    }
                    else if (IsSoloGame && game.CurrentTurn == Player.O)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(250);
                            int aiMove = AiPlayer.GetBestMove(game, Player.O, SelectedDifficulty);
                            if (aiMove != -1)
                            {
                                game.MakeMove(aiMove, Player.O);
                                if (game.Winner != Player.None || game.IsDraw)
                                {
                                    await GameResultService.SaveResultAsync(game);
                                }
                                await InvokeAsync(StateHasChanged);
                            }
                        });
                    }
                }
            }
        }
    }

    private void RestartGame()
    {
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var game))
        {
            _confettiFired = false;
            game.Restart();
        }
    }

    public void Dispose()
    {
        Matchmaking.OnPlayerMatched -= OnMatchedReceived;
        if (MatchId != null && Games.TryGetValue(MatchId.Value, out var g))
        {
            g.OnStateChanged -= OnGameStateChanged;
        }
        _pollTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
