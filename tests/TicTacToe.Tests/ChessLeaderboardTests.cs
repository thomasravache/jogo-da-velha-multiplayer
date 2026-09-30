using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0059: ranking do xadrez — seletor de jogo e consulta por jogo

public class ChessLeaderboardTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    private static BunitContext NewContext(GameResultService service, string uri)
    {
        var ctx = new BunitContext();
        var storage = new InMemoryPlayerStorage();
        storage.Data[PlayerIdentityService.StorageKey] = JsonSerializer.Serialize(new { id = HistoryData.Me, nick = "Eu" });
        ctx.Services.AddSingleton<IPlayerStorage>(storage);
        ctx.Services.AddSingleton<PlayerIdentityService>();
        ctx.Services.AddSingleton(service);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(uri);
        return ctx;
    }

    private static IElement Radio(IRenderedComponent<Leaderboard> cut, string startsWith) =>
        cut.Find("[role='radiogroup'][aria-label='Jogo']").QuerySelectorAll("[role='radio']")
            .Single(r => r.TextContent.Trim().StartsWith(startsWith, StringComparison.Ordinal));

    private static LeaderboardPage ThreePages(LeaderboardQuery q)
    {
        var page = Math.Clamp(q.Page, 1, 3);
        var items = Enumerable.Range((page - 1) * 10 + 1, 10)
            .Select(p => new LeaderboardEntry(p, $"Jogador{p}", 100 - p, 2, 1, 62.5, 3, Now.AddMinutes(-p), false)).ToList();
        return new LeaderboardPage(items, 30, page, 3, null);
    }

    [Fact(DisplayName = "SPEC-0059:CH-01 — Sem ?jogo= o ranking consulta o jogo da velha e mantém as colunas atuais")]
    [Trait("Category", "SPEC-0059:CH-01")]
    public async Task WithoutParameter_ShouldKeepTicTacToeScreen()
    {
        await using var db = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new StubLeaderboardService(db) { Respond = ThreePages };
        await using var ctx = NewContext(svc, "/leaderboard");

        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Equal(10, cut.FindAll("tbody tr").Count), TimeSpan.FromSeconds(3));

        Assert.Equal(GameType.TicTacToe, svc.Queries[0].Game);
        Assert.Equal(8, cut.FindAll("thead th").Count);
    }

    [Theory(DisplayName = "SPEC-0059:UT-01 — Ranking: ?jogo= define o jogo da consulta e o radiogrupo (inválido e ausente caem na velha)")]
    [Trait("Category", "SPEC-0059:UT-01")]
    [InlineData("?jogo=xadrez", GameType.Chess, "Xadrez")]
    [InlineData("?jogo=velha", GameType.TicTacToe, "Jogo da velha")]
    [InlineData("?jogo=go", GameType.TicTacToe, "Jogo da velha")]
    [InlineData("", GameType.TicTacToe, "Jogo da velha")]
    public async Task Leaderboard_ShouldReadGameFromUrl(string query, GameType expected, string selected)
    {
        await using var db = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new StubLeaderboardService(db);
        await using var ctx = NewContext(svc, "/leaderboard" + query);

        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.NotEmpty(svc.Queries), TimeSpan.FromSeconds(3));

        Assert.All(svc.Queries, q => Assert.Equal(expected, q.Game));
        cut.WaitForAssertion(() => Assert.Equal("true", Radio(cut, selected).GetAttribute("aria-checked")));
        Assert.Equal(2, cut.Find("[role='radiogroup'][aria-label='Jogo']").QuerySelectorAll("[role='radio']").Length);
    }

    [Fact(DisplayName = "SPEC-0059:UT-01 — Ranking: trocar de jogo atualiza a URL e volta à primeira página")]
    [Trait("Category", "SPEC-0059:UT-01")]
    public async Task Leaderboard_SwitchingGame_ShouldUpdateUrlAndResetPage()
    {
        await using var db = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new StubLeaderboardService(db) { Respond = ThreePages };
        await using var ctx = NewContext(svc, "/leaderboard");
        var nav = ctx.Services.GetRequiredService<NavigationManager>();

        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Equal(10, cut.FindAll("tbody tr").Count), TimeSpan.FromSeconds(3));
        cut.WaitForAssertion(() => cut.Find("nav[aria-label='Paginação'] button[data-action='next']").Click(), TimeSpan.FromSeconds(3));
        cut.WaitForAssertion(() => Assert.Equal(2, svc.Queries[^1].Page), TimeSpan.FromSeconds(3));

        cut.WaitForAssertion(() => Radio(cut, "Xadrez").Click(), TimeSpan.FromSeconds(3));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(GameType.Chess, svc.Queries[^1].Game);
            Assert.Equal(1, svc.Queries[^1].Page);
        }, TimeSpan.FromSeconds(3));
        Assert.Contains("jogo=xadrez", nav.Uri, StringComparison.Ordinal);
        cut.WaitForAssertion(() => Assert.Equal("true", Radio(cut, "Xadrez").GetAttribute("aria-checked")));
    }

    [Fact(DisplayName = "SPEC-0059:IT-01 — Ranking de xadrez sobre o banco: só xadrez fora do solo, com posição, colunas e 'VOCÊ'")]
    [Trait("Category", "SPEC-0059:IT-01")]
    public async Task ChessLeaderboard_ShouldRankOnlyNonSoloChess()
    {
        var ana = Guid.NewGuid();
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options,
        [
            ChessData.Game("Eu", HistoryData.Me, "Ana", ana, "X", EndReason.Checkmate, Now),
            ChessData.Game("Ana", ana, "Eu", HistoryData.Me, "X", EndReason.Timeout, Now.AddMinutes(-1)),
            ChessData.Game("Eu", HistoryData.Me, "Ana", ana, "X", EndReason.Checkmate, Now.AddMinutes(-2)),
            ChessData.Game("Eu", HistoryData.Me, "Ana", ana, null, EndReason.Stalemate, Now.AddMinutes(-3)),
            ChessData.Game("Eu", HistoryData.Me, "Bot", null, "X", EndReason.Checkmate, Now.AddMinutes(-4), mode: GameMode.Solo),
            new MatchResult
            {
                PlayerXName = "VelhaRival", PlayerOName = "Eu", PlayerXId = Guid.NewGuid(), PlayerOId = HistoryData.Me,
                WinnerSide = "X", WinnerName = "VelhaRival", EndReason = EndReason.Line, PlayedAt = Now, Mode = GameMode.Online,
            },
        ]);
        await using var db = new GameplayDbContext(options);

        var page = await HistoryData.Service(db).GetLeaderboardPageAsync(new LeaderboardQuery(HistoryData.Me, 1, 10, GameType.Chess));

        Assert.Equal(2, page.TotalPlayers);
        Assert.Equal(["Eu", "Ana"], page.Items.Select(i => i.DisplayName).ToArray());
        var me = page.Items[0];
        Assert.Equal((1, 2, 1, 1, 50.0), (me.Position, me.Wins, me.Losses, me.Draws, me.WinRatePercent));
        Assert.True(me.IsMe);
        Assert.Equal(me, page.Me);
        Assert.Equal((2, 1, 2, 1), (page.Items[1].Position, page.Items[1].Wins, page.Items[1].Losses, page.Items[1].Draws));
        Assert.DoesNotContain(page.Items, i => i.DisplayName is "Bot" or "VelhaRival");
    }
}
