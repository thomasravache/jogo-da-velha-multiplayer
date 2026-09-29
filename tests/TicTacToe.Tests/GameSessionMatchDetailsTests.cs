using System;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0036: detalhes da partida no GameSession

public class GameSessionMatchDetailsTests
{
    internal sealed class TestTime(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private static readonly DateTimeOffset Start = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "SPEC-0036:UT-01 — Duração e fim da partida a partir de um relógio injetado")]
    [Trait("Category", "SPEC-0036:UT-01")]
    public void Duration_ShouldComeFromInjectedClock()
    {
        var time = new TestTime(Start);
        using var game = new GameSession(enableBackgroundTimer: false, timeProvider: time);

        Assert.Equal(Start, game.StartedAtUtc);
        Assert.Null(game.Duration);
        Assert.Null(game.EndedAtUtc);

        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        time.Advance(TimeSpan.FromSeconds(30));
        game.MakeMove(2, Player.X);

        Assert.Equal(TimeSpan.FromSeconds(30), game.Duration);
        Assert.Equal(Start.AddSeconds(30), game.EndedAtUtc);

        time.Advance(TimeSpan.FromSeconds(5));
        game.Restart();
        Assert.Equal(Start.AddSeconds(35), game.StartedAtUtc);
        Assert.Null(game.Duration);
    }

    [Fact(DisplayName = "SPEC-0036:UT-02 — MoveCount conta só jogadas válidas e zera no Restart")]
    [Trait("Category", "SPEC-0036:UT-02")]
    public void MoveCount_ShouldCountOnlyValidMoves()
    {
        using var game = new GameSession(enableBackgroundTimer: false);

        game.MakeMove(0, Player.X);
        game.MakeMove(3, Player.O);
        game.MakeMove(1, Player.X);
        game.MakeMove(4, Player.O);
        game.MakeMove(4, Player.X); // casa ocupada: inválida
        game.MakeMove(2, Player.X);

        Assert.Equal(5, game.MoveCount);
        game.Restart();
        Assert.Equal(0, game.MoveCount);
    }

    [Fact(DisplayName = "SPEC-0036:UT-03 — EndReason: linha, empate e estouro do tempo; nulo em andamento")]
    [Trait("Category", "SPEC-0036:UT-03")]
    public void EndReason_ShouldReflectHowTheGameEnded()
    {
        using var running = new GameSession(enableBackgroundTimer: false);
        Assert.Null(running.EndReason);

        using var line = new GameSession(enableBackgroundTimer: false);
        foreach (var (c, p) in new[] { (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X) }) line.MakeMove(c, p);
        Assert.Equal(EndReason.Line, line.EndReason);

        using var draw = new GameSession(enableBackgroundTimer: false);
        foreach (var (c, p) in new[] { (0, Player.X), (1, Player.O), (2, Player.X), (4, Player.O), (3, Player.X), (5, Player.O), (7, Player.X), (6, Player.O), (8, Player.X) }) draw.MakeMove(c, p);
        Assert.Equal(EndReason.Draw, draw.EndReason);

        using var timeout = new GameSession(enableBackgroundTimer: false);
        for (var i = 0; i < GameSession.DefaultTurnTimeSeconds; i++) timeout.Tick();
        Assert.Equal(EndReason.Timeout, timeout.EndReason);
    }

    [Fact(DisplayName = "SPEC-0036:UT-04 — FinalBoard com 9 caracteres X, O ou -")]
    [Trait("Category", "SPEC-0036:UT-04")]
    public void FinalBoard_ShouldRenderNineCharacters()
    {
        using var game = new GameSession(enableBackgroundTimer: false);
        game.MakeMove(4, Player.X);
        game.MakeMove(0, Player.O);

        Assert.Equal("O---X----", game.FinalBoard);
    }

    [Fact(DisplayName = "SPEC-0036:UT-05 — Mode padrão Online e atribuível")]
    [Trait("Category", "SPEC-0036:UT-05")]
    public void Mode_ShouldDefaultToOnline_AndBeAssignable()
    {
        using var game = new GameSession(enableBackgroundTimer: false);
        Assert.Equal(GameMode.Online, game.Mode);

        game.Mode = GameMode.Solo;
        Assert.Equal(GameMode.Solo, game.Mode);
        game.Mode = GameMode.Private;
        Assert.Equal(GameMode.Private, game.Mode);
    }
}
