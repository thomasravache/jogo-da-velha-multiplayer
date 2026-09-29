using System;
using System.Threading.Tasks;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0041: abandonar partida e pedir revanche — regras no GameSession

public class LeaveAndRematchTests
{
    private static GameSession Online(TimeProvider? time = null) =>
        new(enableBackgroundTimer: false, timeProvider: time) { Mode = GameMode.Online };

    private static GameSession Finished(TimeProvider? time = null, GameMode mode = GameMode.Online)
    {
        var g = new GameSession(enableBackgroundTimer: false, timeProvider: time) { Mode = mode };
        SeriesRulesTests.WinRound(g, Player.X);
        return g;
    }

    [Fact(DisplayName = "SPEC-0041:CH-01 — Restart continua reiniciando a partida e zerando tabuleiro, vencedor e timer")]
    [Trait("Category", "SPEC-0041:CH-01")]
    public void Restart_ShouldStillResetTheGame()
    {
        using var g = Finished();
        g.Tick();
        g.Restart();

        Assert.All(g.Board, p => Assert.Equal(Player.None, p));
        Assert.Equal(Player.None, g.Winner);
        Assert.Equal(GameSession.DefaultTurnTimeSeconds, g.RemainingSeconds);
        Assert.Equal(Player.X, g.CurrentTurn);
    }

    [Fact(DisplayName = "SPEC-0041:UT-01 — Abandono online dá a vitória ao oponente por Abandon; em série encerra a série")]
    [Trait("Category", "SPEC-0041:UT-01")]
    public void Leave_InProgress_ShouldForfeit()
    {
        using var g = Online();
        g.MakeMove(0, Player.X);

        Assert.Equal(LeaveResult.Forfeited, g.Leave(Player.X));
        Assert.Equal(Player.O, g.Winner);
        Assert.Equal(EndReason.Abandon, g.EndReason);
        Assert.True(g.HasLeft(Player.X));
        Assert.False(g.HasLeft(Player.O));
        Assert.Equal(1, g.GetScore(Player.O));
        Assert.False(g.MakeMove(1, Player.O));
        Assert.Equal(LeaveResult.Rejected, g.Leave(Player.X)); // já saiu

        using var series = new GameSession(enableBackgroundTimer: false, format: SeriesFormat.BestOf5) { Mode = GameMode.Private };
        series.MakeMove(0, Player.X);
        Assert.Equal(LeaveResult.Forfeited, series.Leave(Player.O));
        Assert.Equal(Player.X, series.Winner);
        Assert.True(series.IsSeriesOver);
        Assert.Equal(Player.X, series.SeriesWinner);
    }

    [Fact(DisplayName = "SPEC-0041:UT-01b — O resultado do abandono é gravável uma única vez")]
    [Trait("Category", "SPEC-0041:UT-01")]
    public void Leave_ForfeitResultIsRecordableOnce()
    {
        using var g = Online();
        g.MakeMove(0, Player.X);
        g.Leave(Player.O);

        Assert.True(g.TryMarkResultRecorded());
        Assert.False(g.TryMarkResultRecorded());
    }

    [Fact(DisplayName = "SPEC-0041:UT-02 — Abandono no solo descarta a partida sem resultado gravável")]
    [Trait("Category", "SPEC-0041:UT-02")]
    public void Leave_Solo_ShouldDiscard()
    {
        using var g = Online();
        g.Mode = GameMode.Solo;
        g.MakeMove(0, Player.X);

        Assert.Equal(LeaveResult.Discarded, g.Leave(Player.X));
        Assert.Equal(Player.None, g.Winner);
        Assert.False(g.TryMarkResultRecorded()); // nada a gravar
        Assert.True(g.HasLeft(Player.X));
    }

    [Fact(DisplayName = "SPEC-0041:UT-03 — Sair depois do fim marca o jogador como ausente e bloqueia a revanche do outro")]
    [Trait("Category", "SPEC-0041:UT-03")]
    public void Leave_AfterEnd_ShouldMarkAbsent()
    {
        using var g = Finished();

        Assert.Equal(LeaveResult.Left, g.Leave(Player.X));
        Assert.True(g.HasLeft(Player.X));
        Assert.Equal(Player.X, g.Winner); // resultado inalterado
        Assert.False(g.RequestRematch(Player.O)); // oponente saiu
        Assert.Equal(RematchState.None, g.RematchState);
    }

    [Fact(DisplayName = "SPEC-0041:UT-04 — Pedido, aceite, recusa, pedidos simultâneos e expiração em 30 s")]
    [Trait("Category", "SPEC-0041:UT-04")]
    public void Rematch_HappyPathsAndExpiry()
    {
        // pedido e aceite
        using var a = Finished();
        Assert.True(a.RequestRematch(Player.X));
        Assert.Equal(RematchState.Requested, a.RematchState);
        Assert.Equal(Player.X, a.RematchRequestedBy);
        Assert.True(a.AcceptRematch(Player.O));
        Assert.Equal(Player.None, a.Winner);
        Assert.All(a.Board, p => Assert.Equal(Player.None, p));
        Assert.Equal(RematchState.None, a.RematchState);
        Assert.Null(a.RematchRequestedBy);

        // recusa
        using var b = Finished();
        b.RequestRematch(Player.X);
        Assert.True(b.DeclineRematch(Player.O));
        Assert.Equal(RematchState.Declined, b.RematchState);
        Assert.Equal(Player.X, b.Winner); // nada reiniciou

        // pedidos simultâneos: aceite automático
        using var c = Finished();
        Assert.True(c.RequestRematch(Player.X));
        Assert.True(c.RequestRematch(Player.O));
        Assert.Equal(Player.None, c.Winner);
        Assert.Equal(RematchState.None, c.RematchState);

        // expiração
        var time = new ManualTime();
        using var d = Finished(time);
        d.RequestRematch(Player.X);
        time.Advance(TimeSpan.FromSeconds(29));
        d.Tick();
        Assert.Equal(RematchState.Requested, d.RematchState);
        time.Advance(TimeSpan.FromSeconds(1));
        d.Tick();
        Assert.Equal(RematchState.Expired, d.RematchState);
        Assert.False(d.AcceptRematch(Player.O));
        Assert.True(d.RequestRematch(Player.O)); // pode pedir de novo
        Assert.Equal(RematchState.Requested, d.RematchState);
        Assert.Equal(Player.O, d.RematchRequestedBy);
    }

    [Fact(DisplayName = "SPEC-0041:UT-05 — Dois aceites simultâneos reiniciam uma vez e comandos inválidos retornam falso sem mudar o estado")]
    [Trait("Category", "SPEC-0041:UT-05")]
    public void Rematch_ConcurrentAcceptsAndInvalidCommands()
    {
        using var g = Finished();
        g.RequestRematch(Player.X);
        var results = new bool[2];
        Parallel.Invoke(() => results[0] = g.AcceptRematch(Player.O), () => results[1] = g.AcceptRematch(Player.O));
        Assert.Single(Array.FindAll(results, r => r));
        Assert.Equal(Player.None, g.Winner);

        // inválidos: partida em andamento
        Assert.False(g.RequestRematch(Player.X));
        Assert.False(g.AcceptRematch(Player.O));
        Assert.False(g.DeclineRematch(Player.O));

        // inválidos: jogador errado e oponente ausente
        using var h = Finished();
        Assert.False(h.AcceptRematch(Player.O)); // sem pedido
        h.RequestRematch(Player.X);
        Assert.False(h.AcceptRematch(Player.X)); // o próprio pedinte
        Assert.False(h.DeclineRematch(Player.X));
        Assert.False(h.RequestRematch(Player.X)); // já pediu
        Assert.False(h.RequestRematch(Player.None));
        Assert.Equal(RematchState.Requested, h.RematchState);
        h.Leave(Player.X); // quem pediu sai
        Assert.False(h.AcceptRematch(Player.O));
        Assert.Equal(RematchState.None, h.RematchState);
    }

    [Fact(DisplayName = "SPEC-0041:UT-06 — Solo reinicia na hora e próxima rodada da série é imediata")]
    [Trait("Category", "SPEC-0041:UT-06")]
    public void Solo_AndSeriesRound_ShouldRestartImmediately()
    {
        using var solo = Finished(mode: GameMode.Solo);
        Assert.True(solo.RequestRematch(Player.X));
        Assert.Equal(Player.None, solo.Winner);
        Assert.Equal(RematchState.None, solo.RematchState);

        using var series = new GameSession(enableBackgroundTimer: false, format: SeriesFormat.BestOf5) { Mode = GameMode.Online };
        SeriesRulesTests.WinRound(series, Player.X);
        Assert.False(series.RequestRematch(Player.X)); // rodada encerrada: use Restart, sem consentimento
        series.Restart();
        Assert.Equal(Player.None, series.Winner);
        Assert.Equal(2, series.RoundNumber);

        for (var i = 0; i < 2; i++)
        {
            SeriesRulesTests.WinRound(series, Player.X);
            if (!series.IsSeriesOver) series.Restart();
        }

        Assert.True(series.IsSeriesOver);
        Assert.True(series.RequestRematch(Player.O)); // nova série exige aceite
        Assert.Equal(RematchState.Requested, series.RematchState);
        Assert.True(series.AcceptRematch(Player.X));
        Assert.False(series.IsSeriesOver);
        Assert.Equal(0, series.GetScore(Player.X));
    }

    [Fact(DisplayName = "SPEC-0041:UT-06b — Sair durante um pedido pendente cancela o pedido")]
    [Trait("Category", "SPEC-0041:UT-06")]
    public void Leave_DuringPendingRequest_ShouldClearIt()
    {
        using var g = Finished();
        g.RequestRematch(Player.X);
        g.Leave(Player.O);

        Assert.Equal(RematchState.None, g.RematchState);
        Assert.False(g.AcceptRematch(Player.O));
    }
}
