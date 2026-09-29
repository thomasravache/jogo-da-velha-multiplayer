using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TicTacToe.Web.Services.Presence;

/// <summary>Informa à partida a queda, o retorno e o fechamento do circuito do jogador.</summary>
public sealed class MatchPresenceCircuitHandler(MatchPresenceContext context) : CircuitHandler
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

    private void Report(bool connected) => context.Session?.SetConnection(context.Player, connected);
}
