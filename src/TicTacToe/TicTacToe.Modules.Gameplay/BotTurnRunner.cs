namespace TicTacToe.Modules.Gameplay;

/// <summary>Executa a jogada do robô após um atraso injetável, se ainda for a vez dele.</summary>
public sealed class BotTurnRunner(TimeProvider time)
{
    public async Task RunAsync(GameSession game, Player bot, AiDifficulty difficulty, TimeSpan delay, CancellationToken ct)
    {
        await Task.Delay(delay, time, ct);

        if (game.CurrentTurn != bot || game.Winner != Player.None || game.IsDraw || game.IsSeriesOver) return;

        var move = AiPlayer.GetBestMove(game, bot, difficulty);
        if (move != -1)
        {
            game.MakeMove(move, bot);
        }
    }
}
