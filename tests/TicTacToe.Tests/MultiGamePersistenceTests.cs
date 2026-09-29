using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0047: generalização multi-jogo — GameType, filtros por jogo e migration aditiva

public class MultiGamePersistenceTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string Migrations = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations");
    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>As partidas 0..4 (mais recentes) viram xadrez; as outras 20 seguem jogo da velha.</summary>
    private static List<MatchResult> Mixed()
    {
        var data = HistoryData.TwentyFive(Now);
        foreach (var m in data.Take(5)) m.GameType = GameType.Chess;
        return data;
    }

    [Fact(DisplayName = "SPEC-0047:CH-01 — GetRecentAsync e GetLeaderboardAsync mantêm ordem e desempate do jogo da velha")]
    [Trait("Category", "SPEC-0047:CH-01")]
    public async Task LegacyQueries_ShouldKeepOrdering()
    {
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options,
        [
            new MatchResult { PlayerXName = "Antiga", PlayerOName = "R", WinnerName = "Antiga", PlayedAt = Now.AddDays(-2) },
            new MatchResult { PlayerXName = "Antiga", PlayerOName = "R", WinnerName = "Antiga", PlayedAt = Now.AddDays(-2).AddMinutes(-1) },
            new MatchResult { PlayerXName = "Recente", PlayerOName = "R", WinnerName = "Recente", PlayedAt = Now },
            new MatchResult { PlayerXName = "Recente", PlayerOName = "R", WinnerName = "Recente", PlayedAt = Now.AddMinutes(-1) },
        ]);
        await using var db = new GameplayDbContext(options);
        var service = HistoryData.Service(db);

        Assert.Equal(["Recente", "Recente", "Antiga", "Antiga"], (await service.GetRecentAsync(10)).Select(m => m.WinnerName!).ToArray());
        Assert.Equal(["Recente", "Antiga"], (await service.GetLeaderboardAsync(10)).Select(r => r.PlayerName).ToArray());
    }

    [Fact(DisplayName = "SPEC-0047:CH-01b — Consultas legadas devolvem só jogo da velha quando há partidas dos dois jogos")]
    [Trait("Category", "SPEC-0047:CH-01")]
    public async Task LegacyQueries_ShouldIgnoreChess()
    {
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options,
        [
            new MatchResult { PlayerXName = "Velha", PlayerOName = "R", WinnerName = "Velha", PlayedAt = Now.AddDays(-1) },
            new MatchResult { PlayerXName = "Xeque", PlayerOName = "R", WinnerName = "Xeque", PlayedAt = Now, GameType = GameType.Chess },
            new MatchResult { PlayerXName = "Xeque", PlayerOName = "R", WinnerName = "Xeque", PlayedAt = Now, GameType = GameType.Chess },
        ]);
        await using var db = new GameplayDbContext(options);
        var service = HistoryData.Service(db);

        Assert.Equal("Velha", Assert.Single(await service.GetRecentAsync(10)).WinnerName);
        Assert.Equal("Velha", Assert.Single(await service.GetLeaderboardAsync(10)).PlayerName);
    }

    [Fact(DisplayName = "SPEC-0047:UT-01 — MatchResult novo é jogo da velha e SaveResultAsync grava TicTacToe")]
    [Trait("Category", "SPEC-0047:UT-01")]
    public async Task NewMatchResult_ShouldDefaultToTicTacToe_AndSaveWritesIt()
    {
        Assert.Equal(GameType.TicTacToe, new MatchResult { PlayerXName = "A", PlayerOName = "B" }.GameType);

        var options = HistoryData.NewOptions();
        await using (var db = new GameplayDbContext(options))
        {
            using var game = new GameSession(enableBackgroundTimer: false);
            SeriesRulesTests.WinRound(game, Player.X);
            await HistoryData.Service(db).SaveResultAsync(game);
        }

        await using var check = new GameplayDbContext(options);
        Assert.Equal(GameType.TicTacToe, Assert.Single(await check.MatchResults.ToListAsync()).GameType);
    }

    [Fact(DisplayName = "SPEC-0047:IT-01 — Linhas sem GameType são lidas como jogo da velha e o modelo declara o padrão 0")]
    [Trait("Category", "SPEC-0047:IT-01")]
    public async Task Model_ShouldDefaultGameTypeToZero_AndReadUnsetRowsAsTicTacToe()
    {
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options,
        [
            new MatchResult { PlayerXName = "A", PlayerOName = "B", PlayedAt = Now },
            new MatchResult { PlayerXName = "C", PlayerOName = "D", PlayedAt = Now, GameType = GameType.Chess },
        ]);
        await using var db = new GameplayDbContext(options);

        var rows = await db.MatchResults.ToListAsync();
        Assert.Equal(GameType.TicTacToe, rows.Single(r => r.PlayerXName == "A").GameType);
        Assert.Equal(GameType.Chess, rows.Single(r => r.PlayerXName == "C").GameType);

        var property = db.Model.FindEntityType(typeof(MatchResult))!.FindProperty(nameof(MatchResult.GameType))!;
        Assert.True(property.TryGetDefaultValue(out var defaultValue));
        Assert.Equal(0, Convert.ToInt32(defaultValue, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains(property.GetContainingIndexes(), i => i.Properties.Count == 1);
    }

    [Fact(DisplayName = "SPEC-0047:IT-02 — Histórico e resumo filtram pelo jogo e as contagens refletem só o jogo pedido")]
    [Trait("Category", "SPEC-0047:IT-02")]
    public async Task HistoryAndSummary_ShouldFilterByGame()
    {
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options, Mixed());
        await using var db = new GameplayDbContext(options);
        var service = HistoryData.Service(db);

        var velha = await service.GetHistoryAsync(HistoryData.Query(pageSize: 50));
        Assert.Equal(20, velha.TotalItems);
        Assert.Equal(20, velha.Counts.All);
        Assert.Equal(HistoryData.Ids(Enumerable.Range(5, 20).ToArray()), velha.Items.Select(i => i.Id).ToArray());

        var chessQuery = HistoryData.Query(pageSize: 50) with { Game = GameType.Chess };
        var chess = await service.GetHistoryAsync(chessQuery);
        Assert.Equal(5, chess.TotalItems);
        Assert.Equal(5, chess.Counts.All);
        Assert.Equal(HistoryData.Ids(0, 1, 2, 3, 4), chess.Items.Select(i => i.Id).ToArray());

        var global = await service.GetHistoryAsync(HistoryData.Query(scope: HistoryScope.All, pageSize: 50) with { Game = GameType.Chess });
        Assert.Equal(5, global.TotalItems);

        Assert.Equal(20, (await service.GetPlayerSummaryAsync(HistoryData.Me)).Total);
        Assert.Equal(20, (await service.GetPlayerSummaryAsync(HistoryData.Me, GameType.TicTacToe)).Total);
        Assert.Equal(5, (await service.GetPlayerSummaryAsync(HistoryData.Me, GameType.Chess)).Total);
    }

    [Fact(DisplayName = "SPEC-0047:IT-02b — Ranking filtra pelo jogo e o padrão é jogo da velha")]
    [Trait("Category", "SPEC-0047:IT-02")]
    public async Task Leaderboard_ShouldFilterByGame()
    {
        var data = LeaderboardData.Players(3, Now);
        var chessId = Guid.NewGuid();
        for (var i = 0; i < 4; i++)
        {
            data.Add(new MatchResult
            {
                PlayerXName = "Xeque",
                PlayerOName = "Feeder",
                PlayerXId = chessId,
                PlayerOId = LeaderboardData.Feeder,
                WinnerName = "Xeque",
                WinnerSide = "X",
                Mode = GameMode.Online,
                EndReason = EndReason.Line,
                PlayedAt = Now.AddMinutes(-i),
                GameType = GameType.Chess,
            });
        }

        var service = await LeaderboardData.Service(HistoryData.NewOptions(), data);

        var velha = await service.GetLeaderboardPageAsync(new LeaderboardQuery(null, 1));
        Assert.Equal(3, velha.TotalPlayers);
        Assert.DoesNotContain(velha.Items, e => e.DisplayName == "Xeque");

        var chess = await service.GetLeaderboardPageAsync(new LeaderboardQuery(null, 1, 10, GameType.Chess));
        Assert.Equal("Xeque", Assert.Single(chess.Items).DisplayName);
        Assert.Equal(4, chess.Items[0].Wins);
    }

    [Fact(DisplayName = "SPEC-0047:IT-03 — Migration AddGameType é só AddColumn com padrão 0 e CreateIndex")]
    [Trait("Category", "SPEC-0047:IT-03")]
    public void Migration_ShouldBeAdditive()
    {
        var file = Directory.GetFiles(Migrations, "*_AddGameType.cs").Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var full = File.ReadAllText(file);
        var up = full[full.IndexOf("void Up(", StringComparison.Ordinal)..full.IndexOf("void Down(", StringComparison.Ordinal)];

        Assert.Equal(1, Regex.Count(up, @"AddColumn<int>"));
        Assert.Contains("defaultValue: 0", up);
        Assert.Contains("nullable: false", up);
        Assert.Equal(1, Regex.Count(up, @"CreateIndex\("));
        Assert.Contains("IX_MatchResults_GameType", up);
        Assert.DoesNotContain("DropColumn", up);
        Assert.DoesNotContain("AlterColumn", up);
        Assert.DoesNotContain("DropTable", up);
    }
}
