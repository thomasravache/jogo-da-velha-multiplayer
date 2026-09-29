using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Web.Services.Chess;

/// <summary>
/// Mapeia <see cref="ChessSession"/> para <see cref="ChessMatchRecord"/> e grava uma vez por partida (SPEC-0053).
/// Deve gravar antes da revanche: <c>RestartCore</c> zera a marca de gravação e o estado da partida.
/// </summary>
public class ChessResultRecorder(GameResultService results, ILogger<ChessResultRecorder> logger)
{
    /// <summary>Motivo de gravação correspondente ao motivo de encerramento da sessão.</summary>
    public static EndReason MapReason(ChessEndReason reason) => reason switch
    {
        ChessEndReason.Checkmate => EndReason.Checkmate,
        ChessEndReason.Stalemate => EndReason.Stalemate,
        ChessEndReason.InsufficientMaterial => EndReason.Insufficient,
        ChessEndReason.FiftyMoveRule => EndReason.FiftyMoves,
        ChessEndReason.ThreefoldRepetition => EndReason.Repetition,
        ChessEndReason.Timeout => EndReason.Timeout,
        ChessEndReason.Resignation or ChessEndReason.Abandon => EndReason.Abandon,
        ChessEndReason.Disconnect => EndReason.Disconnect,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Motivo de xadrez sem mapeamento."),
    };

    /// <summary>Grava o resultado uma única vez; verdadeiro só se esta chamada gravou. Falhas vão para o log.</summary>
    public async Task<bool> SaveOnceAsync(ChessSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var sessionId = session.Id;
        try
        {
            // Lê tudo ANTES de consumir a marca: se a revanche reiniciar a sessão depois, os dados já são desta partida.
            // Snapshot também aplica a bandeira caída, então Result reflete o estado real.
            var snapshot = session.Snapshot();
            if (snapshot.Result is not { } result)
            {
                return false;
            }

            var whiteId = session.GetPlayerId(PieceColor.White);
            var blackId = session.GetPlayerId(PieceColor.Black);
            if (!session.TryMarkResultRecorded())
            {
                return false;
            }

            var record = new ChessMatchRecord(
                snapshot.WhiteName,
                snapshot.BlackName,
                whiteId,
                blackId,
                result.Outcome switch
                {
                    ChessOutcome.WhiteWins => "X",
                    ChessOutcome.BlackWins => "O",
                    _ => null,
                },
                MapReason(result.Reason),
                snapshot.Moves.Count,
                snapshot.EndedAtUtc is { } ended ? (int)Math.Round((ended - snapshot.StartedAtUtc).TotalSeconds) : 0,
                snapshot.Control.Id,
                string.Join(' ', snapshot.Moves.Select(m => m.San)),
                snapshot.Position.ToFen(),
                snapshot.Mode switch
                {
                    ChessMode.Private => GameMode.Private,
                    ChessMode.Solo => GameMode.Solo,
                    _ => GameMode.Online,
                });

            await results.SaveChessAsync(record);
            // SaveChessAsync engole falhas de banco (só registra): este log não garante que a linha foi gravada.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Partida de xadrez {SessionId} gravada. Reason={Reason}", sessionId, record.Reason);
            }

            return true;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Falha ao gravar a partida de xadrez {SessionId}. A partida continuou normalmente.", sessionId);
            }

            return false;
        }
    }
}
