using System;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0042: W.O. por desconexão do oponente — regras no GameSession

public class DisconnectForfeitTests
{
    private static GameSession Online(ManualTime time, SeriesFormat format = SeriesFormat.Single) =>
        new(enableBackgroundTimer: false, timeProvider: time, format: format) { Mode = GameMode.Online };

    private static void Pass(GameSession g, ManualTime time, int seconds)
    {
        time.Advance(TimeSpan.FromSeconds(seconds));
        g.Tick();
    }

    [Fact(DisplayName = "SPEC-0042:CH-01 — Sem eventos de presença o Tick segue o timer de turno e o W.O. por tempo")]
    [Trait("Category", "SPEC-0042:CH-01")]
    public void Tick_WithoutPresence_KeepsTurnTimerBehavior()
    {
        var time = new ManualTime();
        using var g = Online(time);
        g.MakeMove(0, Player.X);
        for (var i = 0; i < GameSession.DefaultTurnTimeSeconds; i++) g.Tick();

        Assert.Equal(Player.X, g.Winner);
        Assert.Equal(EndReason.Timeout, g.EndReason);
        Assert.Null(g.DisconnectSecondsLeft(Player.O));
    }

    [Fact(DisplayName = "SPEC-0042:UT-01 — Queda e retorno em 5 s mantêm a partida; 15 s de queda dão W.O. por Disconnect")]
    [Trait("Category", "SPEC-0042:UT-01")]
    public void Disconnect_ReturnKeepsGame_ExpiryForfeits()
    {
        var time = new ManualTime();
        using var g = Online(time);
        g.SetConnection(Player.O, false);
        Assert.Equal(15, g.DisconnectSecondsLeft(Player.O));
        Pass(g, time, 5);
        Assert.Equal(10, g.DisconnectSecondsLeft(Player.O));

        g.SetConnection(Player.O, true);
        Assert.Null(g.DisconnectSecondsLeft(Player.O));
        Pass(g, time, 30);
        Assert.Equal(Player.None, g.Winner); // contagem cancelada

        g.SetConnection(Player.O, false);
        Pass(g, time, 14);
        Assert.Equal(Player.None, g.Winner);
        Pass(g, time, 1);
        Assert.Equal(Player.X, g.Winner);
        Assert.Equal(EndReason.Disconnect, g.EndReason);
        Assert.Equal(1, g.GetScore(Player.X));
        Assert.False(g.MakeMove(0, Player.X));
    }

    [Fact(DisplayName = "SPEC-0042:UT-01b — Em série a desconexão encerra a série")]
    [Trait("Category", "SPEC-0042:UT-01")]
    public void Disconnect_InSeries_EndsTheSeries()
    {
        var time = new ManualTime();
        using var g = Online(time, SeriesFormat.BestOf5);
        g.SetConnection(Player.X, false);
        Pass(g, time, 15);

        Assert.Equal(Player.O, g.Winner);
        Assert.True(g.IsSeriesOver);
        Assert.Equal(Player.O, g.SeriesWinner);
    }

    [Fact(DisplayName = "SPEC-0042:UT-02 — Solo e partida encerrada ignoram presença; com dois desconectados vale o primeiro a estourar")]
    [Trait("Category", "SPEC-0042:UT-02")]
    public void Presence_IgnoredWhenSoloOrEnded_AndFirstToExpireLoses()
    {
        var time = new ManualTime();
        using var solo = new GameSession(enableBackgroundTimer: false, timeProvider: time) { Mode = GameMode.Solo };
        solo.SetConnection(Player.X, false);
        Assert.Null(solo.DisconnectSecondsLeft(Player.X));
        Pass(solo, time, 20);
        Assert.Equal(Player.None, solo.Winner);

        using var ended = new GameSession(enableBackgroundTimer: false, timeProvider: time) { Mode = GameMode.Online };
        SeriesRulesTests.WinRound(ended, Player.X);
        ended.SetConnection(Player.O, false);
        Assert.Null(ended.DisconnectSecondsLeft(Player.O));
        Assert.Equal(EndReason.Line, ended.EndReason);

        using var both = Online(time);
        both.SetConnection(Player.X, false);
        Pass(both, time, 5);
        both.SetConnection(Player.O, false);
        Pass(both, time, 10); // X completa 15 s, O só 10
        Assert.Equal(Player.O, both.Winner);
        Assert.Equal(EndReason.Disconnect, both.EndReason);
        Pass(both, time, 30);
        Assert.Equal(1, both.GetScore(Player.O));
        Assert.Equal(0, both.GetScore(Player.X)); // sem segundo encerramento
    }

    [Fact(DisplayName = "SPEC-0042:UT-03 — Forfeit concede a vitória ao outro lado, é idempotente e Leave usa o mesmo caminho")]
    [Trait("Category", "SPEC-0042:UT-03")]
    public void Forfeit_IsTheSinglePath()
    {
        using var g = new GameSession(enableBackgroundTimer: false) { Mode = GameMode.Online };
        Assert.True(g.Forfeit(Player.X, EndReason.Disconnect));
        Assert.Equal(Player.O, g.Winner);
        Assert.Equal(EndReason.Disconnect, g.EndReason);
        Assert.False(g.Forfeit(Player.O, EndReason.Abandon));
        Assert.False(g.Forfeit(Player.None, EndReason.Abandon));
        Assert.Equal(1, g.GetScore(Player.O));

        using var left = new GameSession(enableBackgroundTimer: false) { Mode = GameMode.Online };
        left.Leave(Player.X);
        Assert.Equal(g.Winner, left.Winner);
        Assert.Equal(EndReason.Abandon, left.EndReason);
        Assert.Equal(1, left.GetScore(Player.O));

        using var series = new GameSession(enableBackgroundTimer: false, format: SeriesFormat.BestOf5) { Mode = GameMode.Online };
        Assert.True(series.Forfeit(Player.O, EndReason.Disconnect));
        Assert.True(series.IsSeriesOver);
        Assert.Equal(Player.X, series.SeriesWinner);
    }

    [Fact(DisplayName = "SPEC-0042:UT-03b — Quem desistiu não pode pedir nem aceitar revanche")]
    [Trait("Category", "SPEC-0042:UT-03")]
    public void Forfeit_MarksLoserAsGone()
    {
        using var g = new GameSession(enableBackgroundTimer: false) { Mode = GameMode.Online };
        g.Forfeit(Player.X, EndReason.Disconnect);

        Assert.True(g.HasLeft(Player.X));
        Assert.False(g.RequestRematch(Player.O));
    }

    [Fact(DisplayName = "SPEC-0042:UT-02b — Rodada encerrada com o oponente já desconectado não deixa contagem obsoleta")]
    [Trait("Category", "SPEC-0042:UT-02")]
    public void EndedRound_ShouldNotReportStaleCountdown()
    {
        var time = new ManualTime();
        using var g = Online(time);
        g.SetConnection(Player.O, false);
        SeriesRulesTests.WinRound(g, Player.X);

        Assert.Null(g.DisconnectSecondsLeft(Player.O));
    }
}
