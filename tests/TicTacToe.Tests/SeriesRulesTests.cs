using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0040: série melhor de 5 — regras no GameSession e BotTurnRunner

internal sealed class ManualTime : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(callback, state, _now + dueTime);
        lock (_timers) _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        _now += by;
        List<ManualTimer> due;
        lock (_timers) due = _timers.FindAll(t => !t.Done && t.DueAt <= _now);
        foreach (var t in due) t.Fire();
    }

    private sealed class ManualTimer(TimerCallback callback, object? state, DateTimeOffset dueAt) : ITimer
    {
        public DateTimeOffset DueAt { get; } = dueAt;

        public bool Done { get; private set; }

        public void Fire()
        {
            if (Done) return;
            Done = true;
            callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose() => Done = true;

        public ValueTask DisposeAsync()
        {
            Done = true;
            return ValueTask.CompletedTask;
        }
    }
}

public class SeriesRulesTests
{
    private static GameSession Series() => new(enableBackgroundTimer: false, format: SeriesFormat.BestOf5);

    /// <summary>Joga a rodada atual até <paramref name="winner"/> completar a linha 0-1-2, seja quem for que abra.</summary>
    internal static void WinRound(GameSession g, Player winner)
    {
        var loser = winner == Player.X ? Player.O : Player.X;
        int[] w = [0, 1, 2];
        int[] l = [3, 4, 6];
        int wi = 0, li = 0;
        while (g.Winner == Player.None)
        {
            Assert.True(g.CurrentTurn == winner ? g.MakeMove(w[wi++], winner) : g.MakeMove(l[li++], loser));
        }
    }

    /// <summary>Joga a rodada atual até o empate, seja quem for que abra.</summary>
    internal static void DrawRound(GameSession g)
    {
        var starter = g.CurrentTurn;
        var other = starter == Player.X ? Player.O : Player.X;
        int[] a = [0, 1, 5, 6, 8];
        int[] b = [2, 3, 4, 7];
        for (var i = 0; i < 9; i++)
        {
            Assert.True(i % 2 == 0 ? g.MakeMove(a[i / 2], starter) : g.MakeMove(b[i / 2], other));
        }

        Assert.True(g.IsDraw);
    }

    private static void TimeOut(GameSession g)
    {
        for (var i = 0; i < GameSession.DefaultTurnTimeSeconds; i++) g.Tick();
    }

    [Fact(DisplayName = "SPEC-0040:CH-01 — Partida única: placar acumulado, Restart com X abrindo e regras de vitória, empate e W.O.")]
    [Trait("Category", "SPEC-0040:CH-01")]
    public void SingleFormat_ShouldKeepCurrentBehavior()
    {
        using var g = new GameSession(enableBackgroundTimer: false);
        WinRound(g, Player.X);
        Assert.Equal(1, g.GetScore(Player.X));
        g.Restart();
        Assert.Equal(Player.X, g.CurrentTurn);
        WinRound(g, Player.X); // X abre de novo
        Assert.Equal(2, g.GetScore(Player.X));
        g.Restart();
        DrawRound(g);
        Assert.Equal(2, g.GetScore(Player.X));
        Assert.Equal(0, g.GetScore(Player.O));
        g.Restart();
        g.MakeMove(0, Player.X);
        TimeOut(g); // O estoura o tempo → X vence por W.O.
        Assert.Equal(Player.X, g.Winner);
        Assert.Equal(EndReason.Timeout, g.EndReason);
        Assert.Equal(3, g.GetScore(Player.X));
    }

    [Fact(DisplayName = "SPEC-0040:UT-01 — Sem série: formato Single, sem SeriesId e Restart reinicia mesmo com rodada em andamento")]
    [Trait("Category", "SPEC-0040:UT-01")]
    public void Single_ShouldHaveNoSeriesState()
    {
        using var g = new GameSession(enableBackgroundTimer: false);

        Assert.Equal(SeriesFormat.Single, g.Format);
        Assert.Null(g.SeriesId);
        Assert.Equal(1, g.RoundNumber);
        Assert.False(g.IsSeriesOver);
        Assert.False(g.IsMatchPoint(Player.X));

        g.MakeMove(4, Player.X);
        g.Restart(); // comportamento atual: reinicia sempre
        Assert.Equal(Player.None, g.Board[4]);
        Assert.Equal(Player.X, g.CurrentTurn);
    }

    [Fact(DisplayName = "SPEC-0040:UT-02 — Vitória por linha e por W.O. pontuam e avançam a rodada")]
    [Trait("Category", "SPEC-0040:UT-02")]
    public void Series_ShouldScoreLineAndWalkOver()
    {
        using var g = Series();
        Assert.Equal(SeriesFormat.BestOf5, g.Format);
        Assert.NotNull(g.SeriesId);
        Assert.Equal(3, g.SeriesTarget);
        Assert.Equal(1, g.RoundNumber);

        WinRound(g, Player.X);
        Assert.Equal(1, g.GetScore(Player.X));
        Assert.Equal(2, g.RoundNumber);

        g.Restart();
        Assert.Equal(Player.O, g.CurrentTurn);
        g.MakeMove(0, Player.O);
        TimeOut(g); // X estoura o tempo → O vence por W.O.
        Assert.Equal(Player.O, g.Winner);
        Assert.Equal(1, g.GetScore(Player.O));
        Assert.Equal(3, g.RoundNumber);
        Assert.False(g.IsSeriesOver);
    }

    [Fact(DisplayName = "SPEC-0040:UT-03 — Empate na série não pontua e repete a rodada")]
    [Trait("Category", "SPEC-0040:UT-03")]
    public void Series_DrawShouldRepeatTheRound()
    {
        using var g = Series();
        DrawRound(g);

        Assert.Equal(0, g.GetScore(Player.X));
        Assert.Equal(0, g.GetScore(Player.O));
        Assert.Equal(1, g.RoundNumber);
        Assert.False(g.IsSeriesOver);

        g.Restart();
        Assert.All(g.Board, p => Assert.Equal(Player.None, p));
        Assert.Equal(1, g.RoundNumber);
        Assert.Equal(Player.X, g.CurrentTurn);
    }

    [Fact(DisplayName = "SPEC-0040:UT-04 — Três vitórias encerram a série; jogadas são rejeitadas; no máximo cinco rodadas decididas")]
    [Trait("Category", "SPEC-0040:UT-04")]
    public void Series_ShouldEndAtThreeWins()
    {
        using var g = Series();
        WinRound(g, Player.X);
        g.Restart();
        WinRound(g, Player.X);
        g.Restart();
        Assert.False(g.IsSeriesOver);
        WinRound(g, Player.X);

        Assert.True(g.IsSeriesOver);
        Assert.Equal(Player.X, g.SeriesWinner);
        Assert.False(g.MakeMove(8, g.CurrentTurn));

        using var long5 = Series();
        foreach (var w in new[] { Player.X, Player.O, Player.X, Player.O, Player.X })
        {
            Assert.False(long5.IsSeriesOver);
            WinRound(long5, w);
            if (!long5.IsSeriesOver) long5.Restart();
        }

        Assert.True(long5.IsSeriesOver);
        Assert.Equal(Player.X, long5.SeriesWinner);
        Assert.Equal(3, long5.GetScore(Player.X));
        Assert.Equal(2, long5.GetScore(Player.O));
    }

    [Fact(DisplayName = "SPEC-0040:UT-05 — Match point com duas vitórias e apagado ao vencer a série")]
    [Trait("Category", "SPEC-0040:UT-05")]
    public void Series_MatchPoint()
    {
        using var g = Series();
        Assert.False(g.IsMatchPoint(Player.X));
        WinRound(g, Player.X);
        Assert.False(g.IsMatchPoint(Player.X));
        g.Restart();
        WinRound(g, Player.X);
        g.Restart();

        Assert.True(g.IsMatchPoint(Player.X));
        Assert.False(g.IsMatchPoint(Player.O));

        WinRound(g, Player.X);
        Assert.False(g.IsMatchPoint(Player.X));
        Assert.False(g.IsMatchPoint(Player.O));
    }

    [Fact(DisplayName = "SPEC-0040:UT-06 — Quem abre alterna a cada rodada decidida e o empate repete o mesmo")]
    [Trait("Category", "SPEC-0040:UT-06")]
    public void Series_RoundStarterAlternates()
    {
        using var g = Series();
        Assert.Equal(Player.X, g.RoundStarter);
        Assert.Equal(Player.X, g.CurrentTurn);

        WinRound(g, Player.O);
        g.Restart();
        Assert.Equal(Player.O, g.RoundStarter);
        Assert.Equal(Player.O, g.CurrentTurn);

        WinRound(g, Player.X);
        g.Restart();
        Assert.Equal(Player.X, g.RoundStarter);

        DrawRound(g);
        g.Restart();
        Assert.Equal(Player.X, g.RoundStarter); // empate: mesmo jogador abre a repetição
        Assert.Equal(Player.X, g.CurrentTurn);
    }

    [Fact(DisplayName = "SPEC-0040:UT-06b — RoundStarter já indica quem abre a próxima rodada assim que a atual é decidida")]
    [Trait("Category", "SPEC-0040:UT-06")]
    public void Series_RoundStarterIsNextOpenerRightAfterRoundEnds()
    {
        using var g = Series();
        WinRound(g, Player.X);
        Assert.Equal(Player.O, g.RoundStarter);
        g.Restart();
        WinRound(g, Player.O);
        Assert.Equal(Player.X, g.RoundStarter);
    }

    [Fact(DisplayName = "SPEC-0040:UT-07 — Restart: rodada em andamento não muda nada; encerrada avança; série encerrada começa nova série")]
    [Trait("Category", "SPEC-0040:UT-07")]
    public void Series_RestartBehavior()
    {
        using var g = Series();
        var events = 0;
        g.OnStateChanged += () => events++;

        g.MakeMove(4, Player.X);
        var before = events;
        g.Restart(); // rodada em andamento: nada
        Assert.Equal(Player.X, g.Board[4]);
        Assert.Equal(before, events);

        g.MakeMove(0, Player.O);
        g.MakeMove(1, Player.X);
        g.MakeMove(3, Player.O);
        g.MakeMove(7, Player.X); // X: 4,1,7 → coluna 1-4-7
        Assert.Equal(Player.X, g.Winner);

        g.Restart();
        g.MakeMove(2, Player.O); // segunda rodada já em andamento (O abre)
        g.Restart(); // "restart concorrente": não avança de novo
        Assert.Equal(Player.O, g.Board[2]);
        Assert.Equal(2, g.RoundNumber);

        var firstId = g.SeriesId;
        using var done = Series();
        var id = done.SeriesId;
        WinRound(done, Player.X);
        done.Restart();
        WinRound(done, Player.X);
        done.Restart();
        WinRound(done, Player.X);
        Assert.True(done.IsSeriesOver);

        done.Restart(); // nova série
        Assert.False(done.IsSeriesOver);
        Assert.Equal(Player.None, done.SeriesWinner);
        Assert.Equal(0, done.GetScore(Player.X));
        Assert.Equal(1, done.RoundNumber);
        Assert.Equal(Player.X, done.CurrentTurn);
        Assert.NotEqual(id, done.SeriesId);
        Assert.NotNull(firstId);
    }

    [Fact(DisplayName = "SPEC-0040:UT-08 — BotTurnRunner joga após o atraso, abre rodada, respeita a vez, o fim da rodada e o cancelamento")]
    [Trait("Category", "SPEC-0040:UT-08")]
    public async Task BotTurnRunner_ShouldPlayAfterDelayWhenItIsStillItsTurn()
    {
        var time = new ManualTime();
        var runner = new BotTurnRunner(time);
        var delay = TimeSpan.FromMilliseconds(250);

        // Depois da jogada humana
        using var game = new GameSession(enableBackgroundTimer: false, timeProvider: time);
        game.MakeMove(0, Player.X);
        var run = runner.RunAsync(game, Player.O, AiDifficulty.Hard, delay, CancellationToken.None);
        Assert.False(run.IsCompleted);
        Assert.Equal(1, game.MoveCount);
        time.Advance(delay);
        await run;
        Assert.Equal(2, game.MoveCount);

        // Abertura de rodada: O é quem abre a rodada 2
        using var series = new GameSession(enableBackgroundTimer: false, timeProvider: time, format: SeriesFormat.BestOf5);
        WinRound(series, Player.X);
        series.Restart();
        Assert.Equal(Player.O, series.CurrentTurn);
        var opening = runner.RunAsync(series, Player.O, AiDifficulty.Easy, delay, CancellationToken.None);
        time.Advance(delay);
        await opening;
        Assert.Equal(1, series.MoveCount);
        Assert.Equal(Player.X, series.CurrentTurn);

        // A vez mudou durante o atraso
        using var changed = new GameSession(enableBackgroundTimer: false, timeProvider: time);
        changed.MakeMove(0, Player.X);
        var late = runner.RunAsync(changed, Player.O, AiDifficulty.Hard, delay, CancellationToken.None);
        changed.Restart(); // volta a ser a vez de X
        time.Advance(delay);
        await late;
        Assert.Equal(0, changed.MoveCount);

        // A rodada terminou durante o atraso
        using var ended = new GameSession(enableBackgroundTimer: false, timeProvider: time);
        ended.MakeMove(0, Player.X);
        var missed = runner.RunAsync(ended, Player.O, AiDifficulty.Hard, delay, CancellationToken.None);
        TimeOut(ended);
        Assert.Equal(Player.X, ended.Winner);
        time.Advance(delay);
        await missed;
        Assert.Equal(1, ended.MoveCount);

        // Cancelamento
        using var cts = new CancellationTokenSource();
        using var cancelled = new GameSession(enableBackgroundTimer: false, timeProvider: time);
        cancelled.MakeMove(0, Player.X);
        var pending = runner.RunAsync(cancelled, Player.O, AiDifficulty.Hard, delay, cts.Token);
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, cancelled.MoveCount);
    }
}
