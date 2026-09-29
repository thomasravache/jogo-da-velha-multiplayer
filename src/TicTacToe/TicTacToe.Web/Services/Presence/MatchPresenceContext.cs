using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Web.Services.Presence;

/// <summary>Associa o circuito do jogador à partida humana em que ele está (escopo: um por circuito).</summary>
public sealed class MatchPresenceContext
{
    public GameSession? Session { get; private set; }

    public Player Player { get; private set; }

    public void Attach(GameSession session, Player player)
    {
        _ = (session, player);
    }

    public void Detach()
    {
        Session = null;
    }
}
