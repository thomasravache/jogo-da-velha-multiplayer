using System;
using System.Collections.Generic;
using System.Linq;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0038: histórico avançado — regras puras (HistoryAnalysis)

public class HistoryAnalysisTests
{
    [Theory(DisplayName = "SPEC-0038:UT-01 — Resultado pelo lado do jogador, não pelo nome")]
    [Trait("Category", "SPEC-0038:UT-01")]
    [InlineData("X", true, HistoryOutcome.Win)]
    [InlineData("X", false, HistoryOutcome.Loss)]
    [InlineData("O", true, HistoryOutcome.Loss)]
    [InlineData("O", false, HistoryOutcome.Win)]
    [InlineData(null, true, HistoryOutcome.Draw)]
    [InlineData(null, false, HistoryOutcome.Draw)]
    public void Classify_ShouldUseThePlayersSide(string? winnerSide, bool iAmX, HistoryOutcome expected) =>
        Assert.Equal(expected, HistoryAnalysis.Classify(winnerSide, iAmX));

    [Theory(DisplayName = "SPEC-0038:UT-02 — Texto do motivo conforme o contrato")]
    [Trait("Category", "SPEC-0038:UT-02")]
    [InlineData(EndReason.Line, "0,1,2", "3 em linha horizontal")]
    [InlineData(EndReason.Line, "3,4,5", "3 em linha horizontal")]
    [InlineData(EndReason.Line, "6,7,8", "3 em linha horizontal")]
    [InlineData(EndReason.Line, "0,3,6", "3 em linha vertical")]
    [InlineData(EndReason.Line, "1,4,7", "3 em linha vertical")]
    [InlineData(EndReason.Line, "2,5,8", "3 em linha vertical")]
    [InlineData(EndReason.Line, "0,4,8", "3 em linha diagonal principal")]
    [InlineData(EndReason.Line, "2,4,6", "3 em linha diagonal secundária")]
    [InlineData(EndReason.Line, null, "3 em linha")]
    [InlineData(EndReason.Draw, null, "Grid completo sem vencedor")]
    [InlineData(EndReason.Timeout, null, "Tempo esgotado")]
    [InlineData(EndReason.Abandon, null, "Abandono")]
    [InlineData(EndReason.Disconnect, null, "Desconexão do oponente")]
    [InlineData(null, null, "—")]
    public void Reason_ShouldFollowTheContract(EndReason? reason, string? line, string expected) =>
        Assert.Equal(expected, HistoryAnalysis.Reason(reason, line));

    [Theory(DisplayName = "SPEC-0038:UT-02b — W.O. é Timeout, Abandon ou Disconnect")]
    [Trait("Category", "SPEC-0038:UT-02")]
    [InlineData(EndReason.Timeout, true)]
    [InlineData(EndReason.Abandon, true)]
    [InlineData(EndReason.Disconnect, true)]
    [InlineData(EndReason.Line, false)]
    [InlineData(EndReason.Draw, false)]
    [InlineData(null, false)]
    public void IsWalkOver_ShouldMatchTheThreeReasons(EndReason? reason, bool expected) =>
        Assert.Equal(expected, HistoryAnalysis.IsWalkOver(reason));

    [Theory(DisplayName = "SPEC-0038:UT-03 — Duração formatada em segundos e minutos")]
    [Trait("Category", "SPEC-0038:UT-03")]
    [InlineData(22, "22s")]
    [InlineData(72, "1m 12s")]
    [InlineData(60, "1m 00s")]
    [InlineData(0, "0s")]
    [InlineData(null, "—")]
    public void FormatDuration_ShouldHandleSecondsMinutesAndNull(int? seconds, string expected) =>
        Assert.Equal(expected, HistoryAnalysis.FormatDuration(seconds));

    /// <summary>Base de exemplo (índice 0 = mais recente): 14 V, 3 D, 1 E; sequência atual = 4.</summary>
    internal static List<SummaryGame> SampleGames()
    {
        // V V V V D V V V V V D V V V D E V V
        var pattern = "WWWWLWWWWWLWWWLEWW";
        var now = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        var games = new List<SummaryGame>();
        for (var i = 0; i < pattern.Length; i++)
        {
            var iAmX = i % 2 == 0;
            var mySide = iAmX ? "X" : "O";
            var otherSide = iAmX ? "O" : "X";
            var (winner, reason) = pattern[i] switch
            {
                'W' => (mySide, i == 1 ? EndReason.Timeout : EndReason.Line),
                'L' => (otherSide, EndReason.Line),
                _ => ((string?)null, EndReason.Draw),
            };
            int? duration = i == 1 ? 4 : 20 + i * 2;
            int? moves = i == 17 ? null : 5;
            games.Add(new SummaryGame(iAmX, winner, reason, duration, moves, now.AddMinutes(-i)));
        }

        return games;
    }

    [Fact(DisplayName = "SPEC-0038:UT-04 — Resumo: total, taxa, sequência, vitória mais rápida e tempo por lance")]
    [Trait("Category", "SPEC-0038:UT-04")]
    public void Summarize_ShouldComputeAllNumbers()
    {
        var summary = HistoryAnalysis.Summarize(SampleGames());

        Assert.Equal(18, summary.Total);
        Assert.Equal(14, summary.Wins);
        Assert.Equal(3, summary.Losses);
        Assert.Equal(1, summary.Draws);
        Assert.Equal(77.8, summary.WinRatePercent);
        Assert.Equal(4, summary.CurrentWinStreak);
        Assert.Equal(20, summary.FastestWinSeconds); // a vitória por Timeout (4s) não conta
        Assert.NotNull(summary.AverageSecondsPerMove);
        Assert.Equal(594.0 / 85, summary.AverageSecondsPerMove!.Value, 6); // 17 partidas com lances; soma de durações 594s; 5 lances cada
    }

    [Fact(DisplayName = "SPEC-0038:UT-04b — Ordem da entrada não altera a sequência e base vazia devolve zeros")]
    [Trait("Category", "SPEC-0038:UT-04")]
    public void Summarize_ShouldNotDependOnInputOrder_AndHandleEmpty()
    {
        var shuffled = SampleGames().OrderBy(g => g.PlayedAtUtc.Ticks % 7).ThenBy(g => g.PlayedAtUtc).ToList();
        Assert.Equal(4, HistoryAnalysis.Summarize(shuffled).CurrentWinStreak);

        var empty = HistoryAnalysis.Summarize([]);
        Assert.Equal(0, empty.Total);
        Assert.Equal(0, empty.Wins);
        Assert.Equal(0, empty.WinRatePercent);
        Assert.Equal(0, empty.CurrentWinStreak);
        Assert.Null(empty.FastestWinSeconds);
        Assert.Null(empty.AverageSecondsPerMove);
    }

    [Fact(DisplayName = "SPEC-0038:UT-04c — Empate ou derrota mais recente zera a sequência")]
    [Trait("Category", "SPEC-0038:UT-04")]
    public void Summarize_ShouldResetStreakOnDrawOrLoss()
    {
        var now = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        SummaryGame Game(int minutesAgo, string? winner) => new(true, winner, winner is null ? EndReason.Draw : EndReason.Line, 30, 5, now.AddMinutes(-minutesAgo));

        Assert.Equal(0, HistoryAnalysis.Summarize([Game(0, null), Game(1, "X"), Game(2, "X")]).CurrentWinStreak);
        Assert.Equal(0, HistoryAnalysis.Summarize([Game(0, "O"), Game(1, "X")]).CurrentWinStreak);
        Assert.Equal(2, HistoryAnalysis.Summarize([Game(0, "X"), Game(1, "X"), Game(2, "O"), Game(3, "X")]).CurrentWinStreak);
    }
}
