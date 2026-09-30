namespace TicTacToe.Modules.Chess;

/// <summary>Executa o lance do robô após um atraso injetável, se ainda for a vez dele (SPEC-0058).</summary>
public sealed class ChessBotTurnRunner(TimeProvider time)
{
    public TimeProvider Time { get; } = time;

    public Task RunAsync(ChessSession session, int botSeat, IChessBot bot, TimeSpan delay, CancellationToken ct) =>
        throw new NotImplementedException();
}
