using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TicTacToe.Web.Services.Presence;

/// <summary>Informa à partida a queda, o retorno e o fechamento do circuito do jogador.</summary>
public sealed class MatchPresenceCircuitHandler(MatchPresenceContext context) : CircuitHandler
{
    public MatchPresenceContext Context { get; } = context;
}
