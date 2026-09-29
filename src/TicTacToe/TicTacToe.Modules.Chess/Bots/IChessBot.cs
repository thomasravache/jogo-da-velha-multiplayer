namespace TicTacToe.Modules.Chess;

/// <summary>Robô de xadrez: escolhe um lance a partir de uma posição (ADR-0011).</summary>
public interface IChessBot
{
    string Name { get; }

    /// <summary>Devolve um lance legal, ou nulo somente se a posição não tiver lances legais.</summary>
    Task<Move?> ChooseMoveAsync(Position position, CancellationToken ct);
}
