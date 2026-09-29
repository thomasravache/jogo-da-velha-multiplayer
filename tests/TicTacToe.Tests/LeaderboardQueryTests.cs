using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0039: ranking avançado — consultas sobre o banco (EF InMemory)

internal static class LeaderboardData
{
    public static readonly Guid Feeder = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public static Guid PlayerId(int i) => new(i + 1, 0, 0, new byte[8]);

    /// <summary>Jogador i (0..players-1) tem (players − i) vitórias online contra o "Feeder" e 1 empate; assim P0 lidera e P{players-1} fecha.</summary>
    public static List<MatchResult> Players(int players, DateTime now)
    {
        var list = new List<MatchResult>();
        for (var i = 0; i < players; i++)
        {
            for (var k = 0; k < players - i; k++)
            {
                list.Add(new MatchResult
                {
                    PlayerXName = $"P{i}",
                    PlayerOName = "Feeder",
                    PlayerXId = PlayerId(i),
                    PlayerOId = Feeder,
                    WinnerName = $"P{i}",
                    WinnerSide = "X",
                    Mode = GameMode.Online,
                    EndReason = EndReason.Line,
                    PlayedAt = now.AddMinutes(-(i * 3 + k * 100 + 5)),
                });
            }

            list.Add(new MatchResult
            {
                PlayerXName = $"P{i}",
                PlayerOName = "Feeder",
                PlayerXId = PlayerId(i),
                PlayerOId = Feeder,
                Mode = GameMode.Online,
                EndReason = EndReason.Draw,
                PlayedAt = now.AddMinutes(-(i * 3 + 1)),
            });
        }

        return list;
    }

    public static async Task<GameResultService> Service(DbContextOptions<GameplayDbContext> options, IEnumerable<MatchResult> data)
    {
        await HistoryData.Seed(options, data);
        return HistoryData.Service(new GameplayDbContext(options));
    }
}

public class LeaderboardQueryTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact(DisplayName = "SPEC-0039:CH-01 — GetLeaderboardAsync mantém ordem por vitórias e desempate por último triunfo")]
    [Trait("Category", "SPEC-0039:CH-01")]
    public async Task GetLeaderboardAsync_ShouldKeepLegacyOrdering()
    {
        var service = await LeaderboardData.Service(HistoryData.NewOptions(),
        [
            new MatchResult { PlayerXName = "Antiga", PlayerOName = "R", WinnerName = "Antiga", PlayedAt = Now.AddDays(-2) },
            new MatchResult { PlayerXName = "Antiga", PlayerOName = "R", WinnerName = "Antiga", PlayedAt = Now.AddDays(-2).AddMinutes(-1) },
            new MatchResult { PlayerXName = "Recente", PlayerOName = "R", WinnerName = "Recente", PlayedAt = Now },
            new MatchResult { PlayerXName = "Recente", PlayerOName = "R", WinnerName = "Recente", PlayedAt = Now.AddMinutes(-1) },
        ]);

        var ranks = await service.GetLeaderboardAsync(10);

        Assert.Equal(["Recente", "Antiga"], ranks.Select(r => r.PlayerName).ToArray());
    }

    [Fact(DisplayName = "SPEC-0039:IT-01 — Páginas 1 e 2 com 25 jogadores: itens, totais e Me fora da página")]
    [Trait("Category", "SPEC-0039:IT-01")]
    public async Task Pages_ShouldSliceAndExposeMeOutsideThePage()
    {
        var service = await LeaderboardData.Service(HistoryData.NewOptions(), LeaderboardData.Players(25, Now));
        var me = LeaderboardData.PlayerId(15); // 16ª posição

        var first = await service.GetLeaderboardPageAsync(new LeaderboardQuery(me, 1));
        var second = await service.GetLeaderboardPageAsync(new LeaderboardQuery(me, 2));

        Assert.Equal(Enumerable.Range(0, 10).Select(i => $"P{i}"), first.Items.Select(e => e.DisplayName));
        Assert.Equal(Enumerable.Range(10, 10).Select(i => $"P{i}"), second.Items.Select(e => e.DisplayName));
        Assert.Equal(Enumerable.Range(11, 10), second.Items.Select(e => e.Position));
        Assert.Equal(25, first.TotalPlayers); // o Feeder nunca vence e não entra
        Assert.Equal(3, first.PageCount);

        Assert.DoesNotContain(first.Items, e => e.IsMe);
        Assert.Equal(16, first.Me!.Position);
        Assert.Equal("P15", first.Me.DisplayName);
        Assert.Equal(10, first.Me.Wins);
        Assert.Equal(1, first.Me.Draws);
        Assert.Equal(0, first.Me.Losses);
        Assert.Single(second.Items, e => e.IsMe);

        var top = first.Items[0];
        Assert.Equal((25, 0, 1), (top.Wins, top.Losses, top.Draws));
        Assert.Equal(Math.Round(100.0 * 25 / 26, 1), top.WinRatePercent);
    }

    [Fact(DisplayName = "SPEC-0039:IT-01b — Sem identidade ou sem vitória, Me é nulo; página fora do intervalo é limitada")]
    [Trait("Category", "SPEC-0039:IT-01")]
    public async Task Me_ShouldBeNullWithoutIdentityOrWins_AndPageIsClamped()
    {
        var service = await LeaderboardData.Service(HistoryData.NewOptions(), LeaderboardData.Players(25, Now));

        Assert.Null((await service.GetLeaderboardPageAsync(new LeaderboardQuery(null, 1))).Me);
        Assert.Null((await service.GetLeaderboardPageAsync(new LeaderboardQuery(LeaderboardData.Feeder, 1))).Me); // jogou, nunca venceu
        Assert.Equal(3, (await service.GetLeaderboardPageAsync(new LeaderboardQuery(null, 50))).Page);
    }

    [Fact(DisplayName = "SPEC-0039:IT-02 — Solo e partidas antigas contra robô não entram; normais entram")]
    [Trait("Category", "SPEC-0039:IT-02")]
    public async Task Exclusions_ShouldKeepOnlyNormalGames()
    {
        var bot = AiPlayer.GetBotName(AiDifficulty.Hard);
        var service = await LeaderboardData.Service(HistoryData.NewOptions(),
        [
            new MatchResult { PlayerXName = "Solista", PlayerOName = bot, PlayerXId = LeaderboardData.PlayerId(1), WinnerName = "Solista", WinnerSide = "X", Mode = GameMode.Solo, PlayedAt = Now },
            new MatchResult { PlayerXName = "Veterano", PlayerOName = bot, WinnerName = "Veterano", PlayedAt = Now.AddDays(-5) },
            new MatchResult { PlayerXName = bot, PlayerOName = "Veterano", WinnerName = bot, PlayedAt = Now.AddDays(-4) },
            new MatchResult { PlayerXName = "Normal", PlayerOName = "Rival", PlayerXId = LeaderboardData.PlayerId(2), PlayerOId = LeaderboardData.PlayerId(3), WinnerName = "Normal", WinnerSide = "X", Mode = GameMode.Online, PlayedAt = Now.AddDays(-1) },
        ]);

        var page = await service.GetLeaderboardPageAsync(new LeaderboardQuery(null, 1));

        Assert.Equal("Normal", Assert.Single(page.Items).DisplayName);
        Assert.Equal(1, page.TotalPlayers);
    }

    // Smoke sobre EF InMemory (10 mil linhas); não mede SQL Server.
    [Fact(DisplayName = "SPEC-0039:IT-03 — 10.000 partidas classificadas em menos de 500 ms")]
    [Trait("Category", "SPEC-0039:IT-03")]
    public async Task Query_ShouldBeFastWithTenThousandGames()
    {
        var data = Enumerable.Range(0, 10_000).Select(i => new MatchResult
        {
            PlayerXName = $"P{i % 500}",
            PlayerOName = $"P{(i + 1) % 500}",
            PlayerXId = LeaderboardData.PlayerId(i % 500),
            PlayerOId = LeaderboardData.PlayerId((i + 1) % 500),
            WinnerSide = i % 3 == 0 ? "X" : i % 3 == 1 ? "O" : null,
            WinnerName = i % 3 == 0 ? $"P{i % 500}" : i % 3 == 1 ? $"P{(i + 1) % 500}" : null,
            Mode = GameMode.Online,
            PlayedAt = Now.AddMinutes(-i),
        }).ToList();
        var service = await LeaderboardData.Service(HistoryData.NewOptions(), data);
        await service.GetLeaderboardPageAsync(new LeaderboardQuery(null, 1)); // aquecimento

        var sw = Stopwatch.StartNew();
        var page = await service.GetLeaderboardPageAsync(new LeaderboardQuery(LeaderboardData.PlayerId(7), 2));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 500, $"consulta levou {sw.ElapsedMilliseconds} ms");
        Assert.Equal(500, page.TotalPlayers);
    }
}
