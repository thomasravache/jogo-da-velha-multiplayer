using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Web.Services.Presence;

/// <summary>Associa o circuito do jogador à partida humana em que ele está (escopo: um por circuito).</summary>
public sealed class MatchPresenceContext
{
    private readonly object _gate = new();
    private GameSession? _session;
    private Player _player;
    private Action<bool>? _report;

    public GameSession? Session
    {
        get { lock (_gate) { return _session; } }
    }

    public Player Player
    {
        get { lock (_gate) { return _player; } }
    }

    public (GameSession? Session, Player Player) Current
    {
        get { lock (_gate) { return (_session, _player); } }
    }

    /// <summary>Relator de conexão genérico (partidas de xadrez), quando houver.</summary>
    public Action<bool>? Reporter
    {
        get { lock (_gate) { return _report; } }
    }

    public void Attach(GameSession session, Player player)
    {
        lock (_gate)
        {
            _session = session;
            _player = player;
            _report = null;
        }
    }

    /// <summary>Associa um relator de conexão genérico (partidas de xadrez): recebe false na queda e true no retorno.</summary>
    public void Attach(Action<bool> reportConnection)
    {
        ArgumentNullException.ThrowIfNull(reportConnection);
        lock (_gate)
        {
            _report = reportConnection;
            _session = null;
            _player = Player.None;
        }
    }

    public void Detach()
    {
        lock (_gate)
        {
            _session = null;
            _player = Player.None;
            _report = null;
        }
    }
}
