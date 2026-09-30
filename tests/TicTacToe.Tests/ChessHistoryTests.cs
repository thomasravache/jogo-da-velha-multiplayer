using System;
using System.Collections.Generic;
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

// SPEC-0059: histórico do xadrez — seletor de jogo, colunas, motivos, resumo e jornada

internal static class ChessData
{
    public static MatchResult Game(
        string white, Guid? whiteId, string black, Guid? blackId, string? winnerSide, EndReason reason, DateTime playedAt,
        GameMode mode = GameMode.Online, string? timeControl = "blitz5+0", int? moves = 40, int duration = 120) =>
        new()
        {
            PlayerXName = white,
            PlayerOName = black,
            PlayerXId = whiteId,
            PlayerOId = blackId,
            WinnerSide = winnerSide,
            WinnerName = winnerSide switch { "X" => white, "O" => black, _ => null },
            EndReason = reason,
            PlayedAt = playedAt,
            Mode = mode,
            GameType = GameType.Chess,
            TimeControl = timeControl,
            MoveCount = moves,
            DurationSeconds = duration,
            MovesSan = "e4 e5",
            FinalFen = "8/8/8/8/8/8/8/8 w - - 0 1",
        };
}

public class ChessHistoryTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    private static BunitContext NewContext(GameResultService service, string? uri = null)
    {
        var ctx = new BunitContext();
        var storage = new InMemoryPlayerStorage();
        storage.Data[PlayerIdentityService.StorageKey] = JsonSerializer.Serialize(new { id = HistoryData.Me, nick = "Eu" });
        ctx.Services.AddSingleton<IPlayerStorage>(storage);
        ctx.Services.AddSingleton<PlayerIdentityService>();
        ctx.Services.AddSingleton(service);
        if (uri is not null)
        {
            ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(uri);
        }

        return ctx;
    }

    private static IElement Radio(IRenderedComponent<History> cut, string group, string startsWith) =>
        cut.Find($"[role='radiogroup'][aria-label='{group}']").QuerySelectorAll("[role='radio']")
            .Single(r => r.TextContent.Trim().StartsWith(startsWith, StringComparison.Ordinal));

    private static void Retry(IRenderedComponent<History> cut, Action action) =>
        cut.WaitForAssertion(action, TimeSpan.FromSeconds(3));

    private static HistoryPage OneItemPage(HistoryQuery q, HistoryItem item, int pageCount = 1) =>
        new([item], 25, Math.Clamp(q.Page, 1, pageCount), pageCount, new HistoryCounts(25, 10, 10, 5, 5));

    private static HistoryItem VelhaItem() =>
        new(Guid.NewGuid(), "Eu", "Rival", HistoryOutcome.Win, false, "3 em linha horizontal", 30, GameMode.Online, Now, true, "X", "Eu");

    private static HistoryItem ChessItem(bool iAmWhite, HistoryOutcome? outcome, string? tc, int? moves, string reason = "Xeque-mate") =>
        new(Guid.NewGuid(), iAmWhite ? "Eu" : "Rival", iAmWhite ? "Rival" : "Eu", outcome, false, reason, 300, GameMode.Online, Now, iAmWhite,
            outcome switch { HistoryOutcome.Win => iAmWhite ? "X" : "O", HistoryOutcome.Loss => iAmWhite ? "O" : "X", _ => null },
            outcome switch { HistoryOutcome.Win => "Eu", HistoryOutcome.Loss => "Rival", _ => null },
            GameType.Chess, tc, moves);

    [Fact(DisplayName = "SPEC-0059:CH-01 — Sem ?jogo= o histórico consulta o jogo da velha, marca 'Jogo da velha' e não mostra colunas de xadrez")]
    [Trait("Category", "SPEC-0059:CH-01")]
    public async Task WithoutParameter_ShouldKeepTicTacToeScreen()
    {
        await using var recording = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new RecordingGameResultService(recording) { Respond = q => OneItemPage(q, VelhaItem()) };
        await using var ctx = NewContext(svc);

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr")), TimeSpan.FromSeconds(3));

        Assert.Equal(GameType.TicTacToe, svc.Queries[0].Game);
        Assert.DoesNotContain(cut.FindAll("th"), th => th.TextContent.Trim() is "Controle" or "Lances");
        Assert.Contains("Duelo (✕ vs ◯)", cut.Markup);
    }

    [Theory(DisplayName = "SPEC-0059:UT-01 — Histórico: ?jogo= define o jogo da consulta e o radiogrupo (inválido e ausente caem na velha)")]
    [Trait("Category", "SPEC-0059:UT-01")]
    [InlineData("?jogo=xadrez", GameType.Chess, "Xadrez")]
    [InlineData("?jogo=velha", GameType.TicTacToe, "Jogo da velha")]
    [InlineData("?jogo=go", GameType.TicTacToe, "Jogo da velha")]
    [InlineData("", GameType.TicTacToe, "Jogo da velha")]
    public async Task History_ShouldReadGameFromUrl(string query, GameType expected, string selected)
    {
        await using var recording = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new RecordingGameResultService(recording);
        await using var ctx = NewContext(svc, "/history" + query);

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.NotEmpty(svc.Queries), TimeSpan.FromSeconds(3));

        Assert.All(svc.Queries, q => Assert.Equal(expected, q.Game));
        cut.WaitForAssertion(() => Assert.Equal("true", Radio(cut, "Jogo", selected).GetAttribute("aria-checked")));
        Assert.Equal(2, cut.Find("[role='radiogroup'][aria-label='Jogo']").QuerySelectorAll("[role='radio']").Length);
    }

    [Fact(DisplayName = "SPEC-0059:UT-01 — Histórico: trocar de jogo atualiza a URL e volta filtro e página ao início")]
    [Trait("Category", "SPEC-0059:UT-01")]
    public async Task History_SwitchingGame_ShouldUpdateUrlAndResetFilterAndPage()
    {
        await using var recording = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new RecordingGameResultService(recording) { Respond = q => OneItemPage(q, VelhaItem(), pageCount: 3) };
        await using var ctx = NewContext(svc, "/history");
        var nav = ctx.Services.GetRequiredService<NavigationManager>();

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr")), TimeSpan.FromSeconds(3));
        Retry(cut, () => Radio(cut, "Filtrar por resultado", "Vitórias").Click());
        cut.WaitForAssertion(() => Assert.Equal(HistoryFilter.Wins, svc.Queries[^1].Filter), TimeSpan.FromSeconds(3));
        Retry(cut, () => cut.Find("nav[aria-label='Paginação'] button[data-action='next']").Click());
        cut.WaitForAssertion(() => Assert.Equal(2, svc.Queries[^1].Page), TimeSpan.FromSeconds(3));

        Retry(cut, () => Radio(cut, "Jogo", "Xadrez").Click());

        cut.WaitForAssertion(() =>
        {
            var last = svc.Queries[^1];
            Assert.Equal(GameType.Chess, last.Game);
            Assert.Equal(HistoryFilter.All, last.Filter);
            Assert.Equal(1, last.Page);
        }, TimeSpan.FromSeconds(3));
        Assert.Contains("jogo=xadrez", nav.Uri, StringComparison.Ordinal);
        cut.WaitForAssertion(() => Assert.Equal("true", Radio(cut, "Jogo", "Xadrez").GetAttribute("aria-checked")));

        Retry(cut, () => Radio(cut, "Jogo", "Jogo da velha").Click());
        cut.WaitForAssertion(() => Assert.Equal(GameType.TicTacToe, svc.Queries[^1].Game), TimeSpan.FromSeconds(3));
        Assert.Contains("jogo=velha", nav.Uri, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0059:UT-02 — Xadrez: colunas Controle e Lances, cor das peças ao lado do nome e '—' nos nulos")]
    [Trait("Category", "SPEC-0059:UT-02")]
    public async Task ChessItems_ShouldShowControlMovesAndColors()
    {
        await using var recording = new GameplayDbContext(HistoryData.NewOptions());
        HistoryItem[] items =
        [
            ChessItem(iAmWhite: true, HistoryOutcome.Win, "blitz5+0", 67),              // 34 lances completos
            ChessItem(iAmWhite: false, HistoryOutcome.Loss, "rapida10+5", 12),          // 6 lances
            ChessItem(iAmWhite: true, HistoryOutcome.Draw, null, null, "Afogamento"),   // sem dados
        ];
        var svc = new RecordingGameResultService(recording)
        {
            Respond = q => new HistoryPage(items, 3, 1, 1, new HistoryCounts(3, 1, 1, 1, 0)),
        };
        await using var ctx = NewContext(svc, "/history?jogo=xadrez");

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("tbody tr").Count), TimeSpan.FromSeconds(3));

        var headers = cut.FindAll("thead th").Select(h => h.TextContent.Trim()).ToArray();
        Assert.Contains("Controle", headers);
        Assert.Contains("Lances", headers);
        string[] Cells(string cell) => cut.FindAll($"tbody tr [data-cell='{cell}']").Select(c => c.TextContent.Trim()).ToArray();
        Assert.Equal(["Blitz 5+0", "Rápida 10+5", "—"], Cells("controle"));
        Assert.Equal(["34", "6", "—"], Cells("lances"));
        Assert.Equal(["Xeque-mate", "Xeque-mate", "Afogamento"], Cells("motivo"));

        var duels = Cells("duelo");
        Assert.Matches(@"Eu\s*Brancas.*Rival\s*Pretas", duels[0]);
        Assert.Matches(@"Rival\s*Brancas.*Eu\s*Pretas", duels[1]);
        var rows = cut.FindAll("tbody tr");
        Assert.Contains("Vitória", rows[0].TextContent);
        Assert.Contains("Derrota", rows[1].TextContent);
        Assert.Contains("Empate", rows[2].TextContent);
        Assert.DoesNotContain("Deu velha", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0059:UT-02 — Jogo da velha não mostra colunas de xadrez nem cor das peças")]
    [Trait("Category", "SPEC-0059:UT-02")]
    public async Task TicTacToeItems_ShouldNotShowChessColumns()
    {
        await using var recording = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new RecordingGameResultService(recording) { Respond = q => OneItemPage(q, VelhaItem()) };
        await using var ctx = NewContext(svc, "/history?jogo=velha");

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr")), TimeSpan.FromSeconds(3));

        Assert.Empty(cut.FindAll("[data-cell='controle']"));
        Assert.Empty(cut.FindAll("[data-cell='lances']"));
        Assert.DoesNotContain("Brancas", cut.Markup);
        Assert.DoesNotContain("Pretas", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0059:UT-03 — Motivos do xadrez têm o texto do contrato e os do jogo da velha não mudam")]
    [Trait("Category", "SPEC-0059:UT-03")]
    public void Reason_ShouldKnowChessReasons()
    {
        Assert.Equal("Xeque-mate", HistoryAnalysis.Reason(EndReason.Checkmate, null));
        Assert.Equal("Afogamento", HistoryAnalysis.Reason(EndReason.Stalemate, null));
        Assert.Equal("Material insuficiente", HistoryAnalysis.Reason(EndReason.Insufficient, null));
        Assert.Equal("Regra dos 50 lances", HistoryAnalysis.Reason(EndReason.FiftyMoves, null));
        Assert.Equal("Repetição de posição", HistoryAnalysis.Reason(EndReason.Repetition, null));
        Assert.Equal("Tempo esgotado", HistoryAnalysis.Reason(EndReason.Timeout, null));
        Assert.Equal("Abandono", HistoryAnalysis.Reason(EndReason.Abandon, null));
        Assert.Equal("Desconexão do oponente", HistoryAnalysis.Reason(EndReason.Disconnect, null));

        Assert.Equal("3 em linha horizontal", HistoryAnalysis.Reason(EndReason.Line, "0,1,2"));
        Assert.Equal("3 em linha diagonal principal", HistoryAnalysis.Reason(EndReason.Line, "0,4,8"));
        Assert.Equal("Grid completo sem vencedor", HistoryAnalysis.Reason(EndReason.Draw, null));
        Assert.Equal("—", HistoryAnalysis.Reason(null, null));
    }

    [Fact(DisplayName = "SPEC-0059:UT-04 — Resumo: xeque-mate conta na vitória mais rápida; vitória por tempo não")]
    [Trait("Category", "SPEC-0059:UT-04")]
    public void Summarize_ShouldCountCheckmateButNotTimeoutForFastestWin()
    {
        var summary = HistoryAnalysis.Summarize(
        [
            new SummaryGame(true, "X", EndReason.Checkmate, 240, 60, Now),
            new SummaryGame(true, "X", EndReason.Timeout, 30, 20, Now.AddMinutes(-1)),
            new SummaryGame(false, "O", EndReason.Checkmate, 400, 80, Now.AddMinutes(-2)),
        ]);

        Assert.Equal(240, summary.FastestWinSeconds);
        Assert.Equal(3, summary.Wins);

        var onlyTimeout = HistoryAnalysis.Summarize([new SummaryGame(true, "X", EndReason.Timeout, 30, 20, Now)]);
        Assert.Null(onlyTimeout.FastestWinSeconds);

        var velha = HistoryAnalysis.Summarize([new SummaryGame(true, "X", EndReason.Line, 50, 5, Now)]);
        Assert.Equal(50, velha.FastestWinSeconds);
    }

    [Fact(DisplayName = "SPEC-0059:IT-02 — Histórico de xadrez sobre o banco: só xadrez, com contagens, resumo e itens com controle e lances")]
    [Trait("Category", "SPEC-0059:IT-02")]
    public async Task ChessHistory_ShouldReturnOnlyChessWithDetails()
    {
        var other = Guid.NewGuid();
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options,
        [
            ChessData.Game("Eu", HistoryData.Me, "Ana", other, "X", EndReason.Checkmate, Now, moves: 67, duration: 200),
            ChessData.Game("Bia", other, "Eu", HistoryData.Me, null, EndReason.Stalemate, Now.AddMinutes(-1), timeControl: "rapida10+5", moves: 99),
            ChessData.Game("Eu", HistoryData.Me, "Cid", other, "O", EndReason.Timeout, Now.AddMinutes(-2), timeControl: null, moves: null),
            ChessData.Game("Bot", null, "Eu", HistoryData.Me, "O", EndReason.Checkmate, Now.AddMinutes(-3), mode: GameMode.Solo, moves: 30, duration: 90),
            .. HistoryData.TwentyFive(Now),
        ]);
        await using var db = new GameplayDbContext(options);
        var service = HistoryData.Service(db);
        var query = HistoryData.Query() with { Game = GameType.Chess };

        var page = await service.GetHistoryAsync(query);

        Assert.Equal(4, page.TotalItems);
        Assert.Equal(new HistoryCounts(4, 2, 1, 1, 1), page.Counts);
        Assert.All(page.Items, i => Assert.Equal(GameType.Chess, i.Game));
        var mate = page.Items[0];
        Assert.Equal("Xeque-mate", mate.Reason);
        Assert.Equal("blitz5+0", mate.TimeControl);
        Assert.Equal(67, mate.MoveCount);
        Assert.Equal(HistoryOutcome.Win, mate.Outcome);
        Assert.Equal("Afogamento", page.Items[1].Reason);
        Assert.Equal("rapida10+5", page.Items[1].TimeControl);
        Assert.Equal(HistoryOutcome.Draw, page.Items[1].Outcome);
        Assert.Null(page.Items[2].TimeControl);
        Assert.Null(page.Items[2].MoveCount);
        Assert.True(page.Items[2].WalkOver);

        var summary = await service.GetPlayerSummaryAsync(HistoryData.Me, GameType.Chess);
        Assert.Equal(4, summary.Total);
        Assert.Equal(2, summary.Wins);
        Assert.Equal(90, summary.FastestWinSeconds);

        var velha = await service.GetHistoryAsync(HistoryData.Query());
        Assert.Equal(25, velha.TotalItems);
        Assert.All(velha.Items, i => Assert.Equal(GameType.TicTacToe, i.Game));
    }

    [Fact(DisplayName = "SPEC-0059:E2E-01 — Jornada: ranking da velha, troca para xadrez com 'Sua posição', histórico de xadrez filtrado e paginado")]
    [Trait("Category", "SPEC-0059:E2E-01")]
    public async Task Journey_ShouldWalkFromVelhaRankingToChessHistory()
    {
        var rival = Guid.NewGuid();
        var velhaRival = Guid.NewGuid();
        var data = new List<MatchResult>();
        for (var i = 0; i < 12; i++)
        {
            data.Add(ChessData.Game("Eu", HistoryData.Me, "Rival", rival, "X", EndReason.Checkmate, Now.AddMinutes(-i)));
        }

        data.Add(ChessData.Game("Rival", rival, "Eu", HistoryData.Me, "X", EndReason.Timeout, Now.AddMinutes(-20)));
        data.Add(ChessData.Game("Rival", rival, "Eu", HistoryData.Me, null, EndReason.Stalemate, Now.AddMinutes(-21)));
        for (var i = 0; i < 3; i++)
        {
            data.Add(new MatchResult
            {
                PlayerXName = "VelhaRival",
                PlayerOName = "Eu",
                PlayerXId = velhaRival,
                PlayerOId = HistoryData.Me,
                WinnerSide = "X",
                WinnerName = "VelhaRival",
                EndReason = EndReason.Line,
                WinningLine = "0,1,2",
                PlayedAt = Now.AddMinutes(-i),
                Mode = GameMode.Online,
            });
        }

        data.Add(new MatchResult
        {
            PlayerXName = "Eu",
            PlayerOName = "VelhaRival",
            PlayerXId = HistoryData.Me,
            PlayerOId = velhaRival,
            WinnerSide = "X",
            WinnerName = "Eu",
            EndReason = EndReason.Line,
            WinningLine = "0,1,2",
            PlayedAt = Now.AddMinutes(-5),
            Mode = GameMode.Online,
        });

        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options, data);
        await using var db = new GameplayDbContext(options);
        await using var ctx = NewContext(HistoryData.Service(db), "/leaderboard");
        var nav = ctx.Services.GetRequiredService<NavigationManager>();

        var board = ctx.Render<Leaderboard>();
        board.WaitForAssertion(() => Assert.Contains("VelhaRival", board.Find("tbody").TextContent), TimeSpan.FromSeconds(3));
        Assert.Contains("2º", board.Find("section[aria-label='Sua posição']").TextContent);

        board.WaitForAssertion(() => board.Find("[role='radiogroup'][aria-label='Jogo'] [role='radio']:nth-child(2)").Click(), TimeSpan.FromSeconds(3));

        board.WaitForAssertion(() =>
        {
            var body = board.Find("tbody").TextContent;
            Assert.DoesNotContain("VelhaRival", body);
            Assert.Contains("Rival", body);
            Assert.Equal("true", board.Find("tr[data-me]").GetAttribute("data-me"));
            var card = board.Find("section[aria-label='Sua posição']").TextContent;
            Assert.Contains("1º", card);
            Assert.Contains("12 vitórias", card);
        }, TimeSpan.FromSeconds(3));
        Assert.Contains("jogo=xadrez", nav.Uri, StringComparison.Ordinal);

        nav.NavigateTo("/history?jogo=xadrez");
        var history = ctx.Render<History>();
        history.WaitForAssertion(() => Assert.NotEmpty(history.FindAll("tbody tr")), TimeSpan.FromSeconds(3));
        history.WaitForAssertion(() => Radio(history, "Filtrar por resultado", "Vitórias").Click(), TimeSpan.FromSeconds(3));
        history.WaitForAssertion(() => Assert.Equal(10, history.FindAll("tbody tr").Count), TimeSpan.FromSeconds(3));
        Assert.All(history.FindAll("tbody tr [data-cell='motivo']"), c => Assert.Equal("Xeque-mate", c.TextContent.Trim()));

        history.WaitForAssertion(() => history.Find("nav[aria-label='Paginação'] button[data-action='next']").Click(), TimeSpan.FromSeconds(3));
        history.WaitForAssertion(() => Assert.Equal(2, history.FindAll("tbody tr").Count), TimeSpan.FromSeconds(3));
        Assert.Contains("Blitz 5+0", history.Find("tbody").TextContent);
    }
}
