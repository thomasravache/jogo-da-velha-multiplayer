using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Web.Services.Presence;

/// <summary>Informa à partida a queda, o retorno e o fechamento do circuito do jogador.</summary>
public sealed class MatchPresenceCircuitHandler(MatchPresenceContext context, ILogger<MatchPresenceCircuitHandler>? logger = null) : CircuitHandler
{
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Report(false);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Report(true);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Report(false);
        context.Detach();
        return Task.CompletedTask;
    }

    private void Report(bool connected)
    {
        if (context.Reporter is { } reporter)
        {
            reporter(connected); // partidas de xadrez: o relator conhece a sessão e o assento
            return;
        }

        var (session, player) = context.Current;
        if (session is null) return;

        session.SetConnection(player, connected);
        if (logger is not null && logger.IsEnabled(LogLevel.Information))
        {
            var state = connected ? "reconectado" : "desconectado";
            logger.LogInformation(
                "Presença da partida {GameId}: jogador {Player} {State} (tolerância de {Seconds}s).",
                session.Id, player, state, GameSession.DisconnectGraceSeconds);
        }
    }
}
