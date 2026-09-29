using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Pages;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0045: gravação única do resultado da partida

public class SingleResultRecordingTests
{
    private static DbContextOptions<GameplayDbContext> NewOptions() =>
        new DbContextOptionsBuilder<GameplayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static GameSession FinishedGame()
    {
        var game = new GameSession(enableBackgroundTimer: false);
        game.SetPlayerName(Player.X, "Thomas");
        game.SetPlayerName(Player.O, "Ana");
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X);
        return game;
    }

    [Fact(DisplayName = "SPEC-0045:CH-01 — SaveResultAsync grava vitória e empate como antes")]
    [Trait("Category", "SPEC-0045:CH-01")]
    public async Task SaveResultAsync_ShouldKeepWinnerAndDrawBehavior()
    {
        await using var ctx = new GameplayDbContext(NewOptions());
        var service = new GameResultService(ctx, NullLogger<GameResultService>.Instance);

        await service.SaveResultAsync(FinishedGame());

        var row = await ctx.MatchResults.SingleAsync();
        Assert.Equal("Thomas", row.WinnerName);
    }

    [Fact(DisplayName = "SPEC-0045:UT-01 — TryMarkResultRecorded devolve verdadeiro só na primeira chamada")]
    [Trait("Category", "SPEC-0045:UT-01")]
    public void TryMarkResultRecorded_ShouldReturnTrueOnlyOnce()
    {
        var game = FinishedGame();

        Assert.True(game.TryMarkResultRecorded());
        Assert.False(game.TryMarkResultRecorded());
        Assert.False(game.TryMarkResultRecorded());
    }

    [Fact(DisplayName = "SPEC-0045:UT-02 — TryMarkResultRecorded é atômico sob concorrência")]
    [Trait("Category", "SPEC-0045:UT-02")]
    public async Task TryMarkResultRecorded_ShouldBeAtomicUnderConcurrency()
    {
        var game = FinishedGame();

        var results = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => game.TryMarkResultRecorded())));

        Assert.Equal(1, results.Count(r => r));
    }

    [Fact(DisplayName = "SPEC-0045:UT-03 — Restart libera uma nova gravação")]
    [Trait("Category", "SPEC-0045:UT-03")]
    public void Restart_ShouldAllowRecordingAgain()
    {
        var game = FinishedGame();
        Assert.True(game.TryMarkResultRecorded());

        game.Restart();
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X);

        Assert.True(game.TryMarkResultRecorded());
        Assert.False(game.TryMarkResultRecorded());
    }

    [Fact(DisplayName = "SPEC-0045:UT-04 — SaveOnceAsync ignora partida em andamento e grava W.O.")]
    [Trait("Category", "SPEC-0045:UT-04")]
    public async Task SaveOnceAsync_ShouldSkipRunningGameAndSaveTimeout()
    {
        await using var ctx = new GameplayDbContext(NewOptions());
        var service = new GameResultService(ctx, NullLogger<GameResultService>.Instance);

        var running = new GameSession(enableBackgroundTimer: false);
        Assert.False(await service.SaveOnceAsync(running));
        Assert.Empty(ctx.MatchResults);

        var timedOut = new GameSession(enableBackgroundTimer: false);
        for (int i = 0; i < GameSession.DefaultTurnTimeSeconds; i++) timedOut.Tick();
        Assert.True(timedOut.IsTimedOut);
        Assert.True(await service.SaveOnceAsync(timedOut));
        Assert.Single(ctx.MatchResults);
    }

    [Fact(DisplayName = "SPEC-0045:IT-01 — SaveOnceAsync grava uma única linha mesmo em chamadas paralelas")]
    [Trait("Category", "SPEC-0045:IT-01")]
    public async Task SaveOnceAsync_ShouldWriteSingleRow_WhenCalledConcurrently()
    {
        var options = NewOptions();
        var game = FinishedGame();

        await service(options).SaveOnceAsync(game);
        await service(options).SaveOnceAsync(game);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => service(options).SaveOnceAsync(game)));

        await using var check = new GameplayDbContext(options);
        Assert.Equal(1, await check.MatchResults.CountAsync());

        static GameResultService service(DbContextOptions<GameplayDbContext> o) =>
            new(new GameplayDbContext(o), NullLogger<GameResultService>.Instance);
    }

    private static BunitContext CreateHomeContext(DbContextOptions<GameplayDbContext> options)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<MatchmakingService>();
        ctx.Services.AddSingleton<ConcurrentDictionary<Guid, GameSession>>();
        ctx.Services.AddSingleton(new TicTacToe.Web.Components.Ui.ShellState());
        ctx.Services.AddSingleton<TicTacToe.Web.Services.PlayerIdentity.IPlayerStorage>(new InMemoryPlayerStorage());
        ctx.Services.AddScoped<TicTacToe.Web.Services.PlayerIdentity.PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(
            sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return ctx;
    }

    private static void StartOnline(IRenderedComponent<Home> home, string name)
    {
        home.Find("#playerName").Input(name);
        home.FindAll("button").First(b => b.TextContent.Contains("Procurar oponente")).Click();
    }

    [Fact(DisplayName = "SPEC-0045:IT-02 — Dois jogadores observando a mesma partida geram uma única linha")]
    [Trait("Category", "SPEC-0045:IT-02")]
    public async Task TwoPlayersObservingSameGame_ShouldRecordOneRow()
    {
        var options = NewOptions();
        await using var ctx = CreateHomeContext(options);

        var first = ctx.Render<Home>();
        StartOnline(first, "Thomas");
        var second = ctx.Render<Home>();
        StartOnline(second, "Ana");

        var games = ctx.Services.GetRequiredService<ConcurrentDictionary<Guid, GameSession>>();
        var game = Assert.Single(games.Values);

        // X (primeiro a entrar na fila) vence a linha superior; ambos os circuitos recebem o evento.
        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X);

        await Task.Delay(500);

        await using var check = new GameplayDbContext(options);
        Assert.Equal(1, await check.MatchResults.CountAsync());
    }

    [Fact(DisplayName = "SPEC-0045:IT-02b — Partida solo encerrada grava uma única linha")]
    [Trait("Category", "SPEC-0045:IT-02")]
    public async Task SoloGameFinished_ShouldRecordOneRow()
    {
        var options = NewOptions();
        await using var ctx = CreateHomeContext(options);

        var home = ctx.Render<Home>();
        home.Find("#playerName").Input("Thomas");
        home.FindAll("button").First(b => b.TextContent.Contains("Iniciar partida solo")).Click();

        var games = ctx.Services.GetRequiredService<ConcurrentDictionary<Guid, GameSession>>();
        var game = Assert.Single(games.Values);

        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(2, Player.X);

        await Task.Delay(500);

        await using var check = new GameplayDbContext(options);
        Assert.Equal(1, await check.MatchResults.CountAsync());
    }
}
