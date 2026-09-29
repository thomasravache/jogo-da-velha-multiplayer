using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0040: série melhor de 5 — persistência por rodada e migration aditiva

public class SeriesPersistenceTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string Migrations = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations");

    [Fact(DisplayName = "SPEC-0040:IT-01 — Rodadas da série gravam SeriesId, RoundNumber e BestOf; partida única grava nulos")]
    [Trait("Category", "SPEC-0040:IT-01")]
    public async Task SaveResultAsync_ShouldPersistSeriesInfoPerRound()
    {
        var options = HistoryData.NewOptions();
        Guid? seriesId;
        await using (var db = new GameplayDbContext(options))
        {
            var service = HistoryData.Service(db);
            using var series = new GameSession(enableBackgroundTimer: false, format: SeriesFormat.BestOf5);
            seriesId = series.SeriesId;
            SeriesRulesTests.WinRound(series, Player.X);
            await service.SaveResultAsync(series);
            series.Restart();
            SeriesRulesTests.WinRound(series, Player.O);
            await service.SaveResultAsync(series);
            series.Restart();
            SeriesRulesTests.WinRound(series, Player.X);
            await service.SaveResultAsync(series);

            using var single = new GameSession(enableBackgroundTimer: false);
            SeriesRulesTests.WinRound(single, Player.X);
            await service.SaveResultAsync(single);
        }

        await using var check = new GameplayDbContext(options);
        var rows = await check.MatchResults.ToListAsync();
        var rounds = rows.Where(r => r.SeriesId != null).OrderBy(r => r.RoundNumber).ToList();
        Assert.Equal(3, rounds.Count);
        Assert.All(rounds, r => Assert.Equal(seriesId, r.SeriesId));
        Assert.Equal([1, 2, 3], rounds.Select(r => r.RoundNumber));
        Assert.All(rounds, r => Assert.Equal(5, r.BestOf));

        var single1 = rows.Single(r => r.SeriesId == null);
        Assert.Null(single1.RoundNumber);
        Assert.Null(single1.BestOf);
    }

    [Fact(DisplayName = "SPEC-0040:IT-01b — Rodada empatada e a repetição gravam o mesmo número de rodada")]
    [Trait("Category", "SPEC-0040:IT-01")]
    public async Task SaveResultAsync_DrawAndRepeatShareRoundNumber()
    {
        var options = HistoryData.NewOptions();
        await using (var db = new GameplayDbContext(options))
        {
            var service = HistoryData.Service(db);
            using var series = new GameSession(enableBackgroundTimer: false, format: SeriesFormat.BestOf5);
            SeriesRulesTests.DrawRound(series);
            await service.SaveResultAsync(series);
            series.Restart();
            SeriesRulesTests.WinRound(series, Player.X);
            await service.SaveResultAsync(series);
        }

        await using var check = new GameplayDbContext(options);
        Assert.Equal([1, 1], (await check.MatchResults.ToListAsync()).Select(r => r.RoundNumber));
    }

    [Fact(DisplayName = "SPEC-0040:IT-02 — Migration AddSeriesInfo é só AddColumn anulável e linhas antigas continuam legíveis")]
    [Trait("Category", "SPEC-0040:IT-02")]
    public async Task Migration_ShouldBeAdditive_AndOldRowsStillReadable()
    {
        var file = Directory.GetFiles(Migrations, "*_AddSeriesInfo.cs").Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var full = File.ReadAllText(file);
        var up = full[full.IndexOf("void Up(", StringComparison.Ordinal)..full.IndexOf("void Down(", StringComparison.Ordinal)];
        Assert.Equal(3, Regex.Count(up, @"AddColumn<"));
        Assert.Equal(3, Regex.Count(up, @"nullable: true"));
        Assert.DoesNotContain("DropColumn", up);
        Assert.DoesNotContain("AlterColumn", up);
        Assert.DoesNotContain("DropTable", up);

        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options,
        [
            new MatchResult { PlayerXName = "Velho", PlayerOName = "Antigo", WinnerName = "Velho", PlayedAt = DateTime.UtcNow },
        ]);
        await using var db = new GameplayDbContext(options);
        var service = HistoryData.Service(db);

        Assert.Single(await service.GetRecentAsync(10));
        Assert.Equal("Velho", Assert.Single(await service.GetLeaderboardAsync(10)).PlayerName);
    }
}
