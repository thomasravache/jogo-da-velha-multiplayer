using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Web.Services.Chess;

/// <summary>
/// Mapeia <see cref="ChessSession"/> para <see cref="ChessMatchRecord"/> e grava uma vez por partida (SPEC-0053).
/// Deve gravar antes da revanche: <c>RestartCore</c> zera a marca de gravação e o estado da partida.
/// </summary>
public class ChessResultRecorder(GameResultService results, ILogger<ChessResultRecorder> logger)
{
    private readonly GameResultService _results = results;
    private readonly ILogger<ChessResultRecorder> _logger = logger;

    public Task<bool> SaveOnceAsync(ChessSession session) => throw new NotImplementedException();
}
