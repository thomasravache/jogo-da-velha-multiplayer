using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Services.Presence;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0042: adaptador de circuito e registro na aplicação

public class PresenceCircuitHandlerTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact(DisplayName = "SPEC-0042:UT-04 — O handler traduz queda, retorno e fechamento em SetConnection e ignora sem partida")]
    [Trait("Category", "SPEC-0042:UT-04")]
    public async Task Handler_ShouldTranslateCircuitEvents()
    {
        var time = new ManualTime();
        using var game = new GameSession(enableBackgroundTimer: false, timeProvider: time) { Mode = GameMode.Online };
        var context = new MatchPresenceContext();
        var handler = new MatchPresenceCircuitHandler(context);

        await handler.OnConnectionDownAsync(null!, CancellationToken.None); // sem partida: sem efeito
        Assert.Null(game.DisconnectSecondsLeft(Player.O));

        context.Attach(game, Player.O);
        Assert.Same(game, context.Session);
        Assert.Equal(Player.O, context.Player);

        await handler.OnConnectionDownAsync(null!, CancellationToken.None);
        Assert.NotNull(game.DisconnectSecondsLeft(Player.O));
        await handler.OnConnectionUpAsync(null!, CancellationToken.None);
        Assert.Null(game.DisconnectSecondsLeft(Player.O));

        await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
        Assert.NotNull(game.DisconnectSecondsLeft(Player.O));
        Assert.Null(context.Session); // fechar o circuito solta a partida
    }

    [Fact(DisplayName = "SPEC-0042:IT-02 — Program registra o contexto como Scoped e o handler como CircuitHandler; Detach solta a partida")]
    [Trait("Category", "SPEC-0042:IT-02")]
    public void Program_ShouldRegisterPresence()
    {
        var program = File.ReadAllText(Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Program.cs"));
        Assert.Contains("AddScoped<MatchPresenceContext>", program);
        Assert.Contains("AddScoped<CircuitHandler, MatchPresenceCircuitHandler>", program);
        Assert.True(typeof(CircuitHandler).IsAssignableFrom(typeof(MatchPresenceCircuitHandler)));

        using var game = new GameSession(enableBackgroundTimer: false);
        var context = new MatchPresenceContext();
        context.Attach(game, Player.X);
        context.Detach();
        Assert.Null(context.Session);
    }
}
