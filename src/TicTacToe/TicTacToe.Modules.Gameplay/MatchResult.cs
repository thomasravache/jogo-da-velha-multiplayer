using System;

namespace TicTacToe.Modules.Gameplay;

public class MatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string PlayerXName { get; set; }
    public required string PlayerOName { get; set; }
    public string? WinnerName { get; set; }  // null = draw
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
