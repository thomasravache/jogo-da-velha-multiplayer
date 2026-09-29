namespace TicTacToe.Modules.Gameplay;

/// <summary>Executa a jogada do robô após um atraso injetável, se ainda for a vez dele.</summary>
public sealed class BotTurnRunner(TimeProvider time)
{
    public Task RunAsync(GameSession game, Player bot, AiDifficulty difficulty, TimeSpan delay, CancellationToken ct)
    {
        _ = time;
        throw new NotImplementedException();
    }
}
