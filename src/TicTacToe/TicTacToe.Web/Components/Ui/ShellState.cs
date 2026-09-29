namespace TicTacToe.Web.Components.Ui;

/// <summary>Estado do shell por circuito: modo imersivo (partida) e título exibido no header mobile.</summary>
public sealed class ShellState
{
    public bool Immersive { get; private set; }

    public string? Title { get; private set; }

    public event Action? Changed;

    public void Set(bool immersive, string? title = null) => throw new NotImplementedException();

    public void Reset()
    {
        Changed?.Invoke();
        throw new NotImplementedException();
    }
}
