using System;

namespace TicTacToe.Modules.Gameplay;

public enum GameMode { Online = 0, Private = 1, Solo = 2 }

public enum EndReason
{
    Line = 0, Draw = 1, Timeout = 2, Abandon = 3, Disconnect = 4,
    // Motivos do xadrez (SPEC-0053): no máximo 16 caracteres, gravados como texto.
    Checkmate = 5, Stalemate = 6, Insufficient = 7, FiftyMoves = 8, Repetition = 9
}

public enum GameType { TicTacToe = 0, Chess = 1 }

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

    // Jogo da partida (SPEC-0047): partidas antigas valem como jogo da velha.
    public GameType GameType { get; set; } = GameType.TicTacToe;

    // Detalhes do xadrez (SPEC-0053): nulos no jogo da velha e em partidas antigas.
    public string? TimeControl { get; set; }  // "blitz5+0"
    public string? MovesSan { get; set; }     // lances em SAN separados por espaço
    public string? FinalFen { get; set; }
}

/// <summary>Partida de xadrez pronta para gravar; brancas ocupam o lado X e pretas o O (SPEC-0053).</summary>
public record ChessMatchRecord(
    string WhiteName,
    string BlackName,
    Guid? WhiteId,
    Guid? BlackId,
    string? WinnerSide,
    EndReason Reason,
    int MoveCount,
    int DurationSeconds,
    string TimeControl,
    string MovesSan,
    string FinalFen,
    GameMode Mode);
