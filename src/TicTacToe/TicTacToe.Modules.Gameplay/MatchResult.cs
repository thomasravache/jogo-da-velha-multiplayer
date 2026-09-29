using System;

namespace TicTacToe.Modules.Gameplay;

public enum GameMode { Online = 0, Private = 1, Solo = 2 }

public enum EndReason { Line = 0, Draw = 1, Timeout = 2, Abandon = 3, Disconnect = 4 }

public class MatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string PlayerXName { get; set; }
    public required string PlayerOName { get; set; }
    public string? WinnerName { get; set; }  // null = draw
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    // Detalhes da partida (SPEC-0036): nulos em partidas antigas.
    public int? DurationSeconds { get; set; }
    public int? MoveCount { get; set; }
    public EndReason? EndReason { get; set; }
    public string? WinnerSide { get; set; }   // "X" | "O"; nulo em empate
    public string? WinningLine { get; set; }  // "0,4,8"
    public string? FinalBoard { get; set; }   // 9 caracteres: X, O ou -
    public GameMode? Mode { get; set; }

    // Identidade anônima dos jogadores (SPEC-0037): nula em partidas antigas e para o robô.
    public Guid? PlayerXId { get; set; }
    public Guid? PlayerOId { get; set; }

    // Série melhor de 5 (SPEC-0040): nulos em partida única e em partidas antigas.
    public Guid? SeriesId { get; set; }
    public int? RoundNumber { get; set; }
    public int? BestOf { get; set; }
}
