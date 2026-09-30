namespace TicTacToe.Modules.Chess;

/// <summary>
/// Executa o lance do robô após um atraso injetável (SPEC-0058). A cor do robô é lida do assento na hora da
/// jogada, porque a revanche troca as cores; o lance só é jogado se ainda for a vez dele e a partida seguir em andamento.
/// </summary>
public sealed class ChessBotTurnRunner(TimeProvider time)
{
    public async Task RunAsync(ChessSession session, int botSeat, IChessBot bot, TimeSpan delay, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(bot);

        await Task.Delay(delay, time, ct).ConfigureAwait(false);

        var snapshot = session.Snapshot();
        var color = session.ColorOf(botSeat);
        if (snapshot.Result is not null || snapshot.SideToMove != color)
        {
            return;
        }

        var move = await bot.ChooseMoveAsync(snapshot.Position, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        if (move is not { } chosen)
        {
            return;
        }

        // A partida pode ter mudado durante a busca (revanche, lance, fim): só joga sobre a posição que foi analisada.
        var current = session.Snapshot();
        if (current.Result is not null || current.Moves.Count != snapshot.Moves.Count || session.ColorOf(botSeat) != color)
        {
            return;
        }

        session.TryMove(color, chosen.From, chosen.To, chosen.Promotion, out _);
    }
}
