using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0038: histórico avançado — interface (filtros, busca, ordenação, paginação e resumo)

internal sealed class RecordingGameResultService(GameplayDbContext db) : GameResultService(db, NullLogger<GameResultService>.Instance)
{
    private readonly object _gate = new();
    private readonly List<HistoryQuery> _queries = [];

    public Func<HistoryQuery, HistoryPage> Respond { get; set; } = q => HistoryAdvancedUiTests.PageFor(q, 3, 25);

    public PlayerSummary Summary { get; set; } = new(25, 10, 10, 5, 40.0, 2, 79, 17.6);

    public List<HistoryQuery> Queries
    {
        get { lock (_gate) return [.. _queries]; }
    }

    public override Task<HistoryPage> GetHistoryAsync(HistoryQuery query)
    {
        lock (_gate) _queries.Add(query);
        return Task.FromResult(Respond(query));
    }

    public override Task<PlayerSummary> GetPlayerSummaryAsync(Guid playerId) => Task.FromResult(Summary);
}

public class HistoryAdvancedUiTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    internal static HistoryPage PageFor(HistoryQuery q, int pageCount, int total)
    {
        var item = new HistoryItem(Guid.NewGuid(), "Eu", "Rival", HistoryOutcome.Win, false, "3 em linha horizontal", 30,
            GameMode.Online, Now, true, "X", "Eu");
        return new HistoryPage([item], total, Math.Clamp(q.Page, 1, pageCount), pageCount, new HistoryCounts(25, 10, 10, 5, 5));
    }

    private static BunitContext NewContext(GameResultService service, bool withProfile = true)
    {
        var ctx = new BunitContext();
        var storage = new InMemoryPlayerStorage();
        if (withProfile)
        {
            storage.Data[PlayerIdentityService.StorageKey] = JsonSerializer.Serialize(new { id = HistoryData.Me, nick = "Eu" });
        }

        ctx.Services.AddSingleton<IPlayerStorage>(storage);
        ctx.Services.AddSingleton<PlayerIdentityService>();
        ctx.Services.AddSingleton(service);
        return ctx;
    }

    private static async Task<(BunitContext Ctx, GameplayDbContext Db)> RealContext(IEnumerable<MatchResult> data, bool withProfile = true)
    {
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options, data);
        var db = new GameplayDbContext(options);
        return (NewContext(HistoryData.Service(db), withProfile), db);
    }

    private static IElement Radio(IRenderedComponent<History> cut, string group, string startsWith) =>
        cut.Find($"[role='radiogroup'][aria-label='{group}']").QuerySelectorAll("[role='radio']")
            .Single(r => r.TextContent.Trim().StartsWith(startsWith, StringComparison.Ordinal));

    private static string[] Cells(IRenderedComponent<History> cut, string cell) =>
        cut.FindAll($"tbody tr [data-cell='{cell}']").Select(c => c.TextContent.Trim()).ToArray();

    private static void WaitRows(IRenderedComponent<History> cut, int rows) =>
        cut.WaitForAssertion(() => Assert.Equal(rows, cut.FindAll("tbody tr").Count), TimeSpan.FromSeconds(3));

    [Fact(DisplayName = "SPEC-0038:UT-05 — Filtros, busca com debounce, ordenação, paginação e escopo chamam a consulta com os parâmetros certos")]
    [Trait("Category", "SPEC-0038:UT-05")]
    public async Task Controls_ShouldDriveTheQuery()
    {
        await using var recording = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new RecordingGameResultService(recording);
        await using var ctx = NewContext(svc);

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.NotEmpty(svc.Queries), TimeSpan.FromSeconds(3));
        var first = svc.Queries[0];
        Assert.Equal(HistoryData.Me, first.PlayerId);
        Assert.Equal(HistoryScope.Mine, first.Scope);
        Assert.Equal(HistoryFilter.All, first.Filter);
        Assert.Equal(HistorySort.Recent, first.Sort);
        Assert.Equal(1, first.Page);
        Assert.Equal(10, first.PageSize);
        Assert.Null(first.Opponent);

        cut.WaitForAssertion(() => Assert.Equal("true", Radio(cut, "Filtrar por resultado", "Todas").GetAttribute("aria-checked")));
        Radio(cut, "Filtrar por resultado", "Vitórias").Click();
        cut.WaitForAssertion(() => Assert.Equal(HistoryFilter.Wins, svc.Queries[^1].Filter), TimeSpan.FromSeconds(3));
        Assert.Equal("true", Radio(cut, "Filtrar por resultado", "Vitórias").GetAttribute("aria-checked"));
        Assert.Equal("false", Radio(cut, "Filtrar por resultado", "Todas").GetAttribute("aria-checked"));

        cut.Find("nav[aria-label='Paginação'] button[data-action='next']").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, svc.Queries[^1].Page), TimeSpan.FromSeconds(3));
        Assert.Equal(HistoryFilter.Wins, svc.Queries[^1].Filter);

        var before = svc.Queries.Count;
        var search = cut.Find("input#busca-adversario");
        search.Input("a");
        search.Input("an");
        search.Input("ana");
        Assert.Equal(before, svc.Queries.Count); // debounce: nada foi consultado ainda
        cut.WaitForAssertion(() => Assert.Equal("ana", svc.Queries[^1].Opponent), TimeSpan.FromSeconds(3));
        Assert.Equal(1, svc.Queries.Count - before);
        Assert.Equal(1, svc.Queries[^1].Page); // buscar volta à primeira página

        Radio(cut, "Ordenar por", "Mais rápidas").Click();
        cut.WaitForAssertion(() => Assert.Equal(HistorySort.ShortestDuration, svc.Queries[^1].Sort), TimeSpan.FromSeconds(3));
        Radio(cut, "Ordenar por", "Resultado").Click();
        cut.WaitForAssertion(() => Assert.Equal(HistorySort.Result, svc.Queries[^1].Sort), TimeSpan.FromSeconds(3));

        Radio(cut, "Escopo", "Todos").Click();
        cut.WaitForAssertion(() => Assert.Equal(HistoryScope.All, svc.Queries[^1].Scope), TimeSpan.FromSeconds(3));
        var all = svc.Queries[^1];
        Assert.Null(all.PlayerId);
        Assert.Equal(HistoryFilter.All, all.Filter); // Vitórias não vale em Todos: volta para Todas
        Assert.Equal("ana", all.Opponent);
        Assert.Equal(1, all.Page);
        Assert.DoesNotContain(cut.Find("[role='radiogroup'][aria-label='Filtrar por resultado']").QuerySelectorAll("[role='radio']"),
            r => r.TextContent.Contains("Vitórias") || r.TextContent.Contains("Derrotas"));
        Assert.Empty(cut.FindAll("[data-stat]")); // o resumo é pessoal
    }

    [Fact(DisplayName = "SPEC-0038:UT-05c — Resultado vazio por filtro tem estado próprio e 'Limpar filtros' restaura a consulta")]
    [Trait("Category", "SPEC-0038:UT-05")]
    public async Task FilteredEmpty_ShouldOfferClearFilters()
    {
        await using var recording = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new RecordingGameResultService(recording)
        {
            Respond = q => q.Filter == HistoryFilter.All
                ? PageFor(q, 1, 25)
                : new HistoryPage([], 0, 1, 1, new HistoryCounts(25, 10, 10, 5, 5)),
        };
        await using var ctx = NewContext(svc);

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr")), TimeSpan.FromSeconds(3));
        Radio(cut, "Filtrar por resultado", "Derrotas").Click();
        cut.WaitForAssertion(() => Assert.Contains("Nenhuma partida com estes filtros", cut.Markup), TimeSpan.FromSeconds(3));
        Assert.DoesNotContain("Nenhuma partida registrada ainda", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Limpar filtros")).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr")), TimeSpan.FromSeconds(3));
        Assert.Equal(HistoryFilter.All, svc.Queries[^1].Filter);
        Assert.Null(svc.Queries[^1].Opponent);
    }

    [Fact(DisplayName = "SPEC-0038:UT-05d — Respostas fora de ordem: a última consulta prevalece")]
    [Trait("Category", "SPEC-0038:UT-05")]
    public async Task StaleResponses_ShouldBeIgnored()
    {
        await using var recording = new GameplayDbContext(HistoryData.NewOptions());
        var gate = new TaskCompletionSource();
        var svc = new SlowThenFastService(recording, gate);
        await using var ctx = NewContext(svc);

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.Equal(1, svc.Calls), TimeSpan.FromSeconds(3));
        Radio(cut, "Ordenar por", "Mais rápidas").Click(); // segunda consulta responde rápido
        cut.WaitForAssertion(() => Assert.Contains("rápida", cut.Markup), TimeSpan.FromSeconds(3));
        gate.SetResult(); // a primeira (lenta) responde depois e não pode sobrescrever
        await Task.Delay(100);
        Assert.DoesNotContain("LENTA", cut.Markup);
    }

    private sealed class SlowThenFastService(GameplayDbContext db, TaskCompletionSource gate) : GameResultService(db, NullLogger<GameResultService>.Instance)
    {
        private int _calls;

        public int Calls => _calls;

        public override async Task<HistoryPage> GetHistoryAsync(HistoryQuery query)
        {
            var n = System.Threading.Interlocked.Increment(ref _calls);
            var slow = n == 1;
            if (slow) await gate.Task;
            var item = new HistoryItem(Guid.NewGuid(), "Eu", slow ? "LENTA" : "rápida", HistoryOutcome.Win, false, "—", 30, null, Now, true, "X", "Eu");
            return new HistoryPage([item], 1, 1, 1, new HistoryCounts(1, 1, 0, 0, 0));
        }

        public override Task<PlayerSummary> GetPlayerSummaryAsync(Guid playerId) => Task.FromResult(new PlayerSummary(1, 1, 0, 0, 100, 1, 30, 6));
    }

    [Fact(DisplayName = "SPEC-0038:UT-06 — Partidas antigas mostram '—' em duração e motivo, sem erro")]
    [Trait("Category", "SPEC-0038:UT-06")]
    public async Task LegacyRows_ShouldShowDashes()
    {
        var (ctx, db) = await RealContext([new MatchResult { PlayerXName = "Velho", PlayerOName = "Antigo", WinnerName = "Velho", PlayedAt = Now.AddDays(-30) }]);
        await using var _ = db;
        await using var __ = ctx;

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[role='radiogroup'][aria-label='Escopo']")), TimeSpan.FromSeconds(3));
        Radio(cut, "Escopo", "Todos").Click();
        WaitRows(cut, 1);

        Assert.Equal(["—"], Cells(cut, "duracao"));
        Assert.Equal(["—"], Cells(cut, "motivo"));
        Assert.Contains("Vitória de Velho", cut.Find("tbody tr").TextContent);
    }

    [Fact(DisplayName = "SPEC-0038:UT-06b — Escopo pessoal sem partidas mostra estado vazio próprio com 'Jogar agora'")]
    [Trait("Category", "SPEC-0038:UT-06")]
    public async Task MineScope_WithoutGames_ShouldShowOwnEmptyState()
    {
        var (ctx, db) = await RealContext([]);
        await using var _ = db;
        await using var __ = ctx;

        var cut = ctx.Render<History>();
        cut.WaitForAssertion(() => Assert.Contains("Você ainda não jogou nenhuma partida", cut.Markup), TimeSpan.FromSeconds(3));

        Assert.Equal("/", cut.FindAll("a").Single(a => a.TextContent.Contains("Jogar agora")).GetAttribute("href"));
        Assert.Empty(cut.FindAll("[data-stat]"));
    }

    [Fact(DisplayName = "SPEC-0038:UT-06c — Escopo pessoal: chip de resultado, W.O., motivo, duração e resumo do jogador")]
    [Trait("Category", "SPEC-0038:UT-06")]
    public async Task MineScope_ShouldShowPersonalPerspectiveAndSummary()
    {
        var (ctx, db) = await RealContext(HistoryData.TwentyFive(DateTime.UtcNow));
        await using var _ = db;
        await using var __ = ctx;

        var cut = ctx.Render<History>();
        WaitRows(cut, 10);
        var rows = cut.FindAll("tbody tr");

        Assert.Equal("win", rows[0].GetAttribute("data-outcome"));
        Assert.Contains("Vitória", rows[0].QuerySelector("[data-cell='resultado']")!.TextContent);
        Assert.DoesNotContain("W.O.", rows[0].QuerySelector("[data-cell='resultado']")!.TextContent);
        Assert.Equal("loss", rows[4].GetAttribute("data-outcome"));
        Assert.Contains("Derrota", rows[4].QuerySelector("[data-cell='resultado']")!.TextContent);
        Assert.Contains("W.O.", rows[4].QuerySelector("[data-cell='resultado']")!.TextContent);
        Assert.Equal("Tempo esgotado", rows[4].QuerySelector("[data-cell='motivo']")!.TextContent.Trim());
        Assert.Equal("draw", rows[3].GetAttribute("data-outcome"));
        Assert.Contains("Deu velha", rows[3].QuerySelector("[data-cell='resultado']")!.TextContent);
        Assert.Equal("1m 40s", rows[0].QuerySelector("[data-cell='duracao']")!.TextContent.Trim());

        string Stat(string key) => cut.Find($"[data-stat='{key}']").TextContent;
        Assert.Contains("25", Stat("total"));
        Assert.Contains("40,0%", Stat("taxa"));
        Assert.Contains("2", Stat("sequencia"));
        Assert.Contains("1m 19s", Stat("recorde"));
        Assert.Contains("17,6s", Stat("lance"));
        Assert.Contains("Página 1 de 3", cut.Find("nav[aria-label='Paginação']").TextContent);
    }

    [Fact(DisplayName = "SPEC-0038:UT-05e — StatTile mostra rótulo, valor e dica")]
    [Trait("Category", "SPEC-0038:UT-05")]
    public void StatTile_ShouldRenderLabelValueAndHint()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<StatTile>(p => p.Add(c => c.Label, "Taxa de vitória").Add(c => c.Value, "77,8%").Add(c => c.Hint, "14 de 18"));

        Assert.Contains("Taxa de vitória", cut.Markup);
        Assert.Contains("77,8%", cut.Markup);
        Assert.Contains("14 de 18", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0038:UT-05f — Pager: limites desabilitados, clique notifica a página e nav rotulada")]
    [Trait("Category", "SPEC-0038:UT-05")]
    public void Pager_ShouldDisableAtLimitsAndNotify()
    {
        using var ctx = new BunitContext();
        var pages = new List<int>();
        IRenderedComponent<Pager> Render(int page, int count) => ctx.Render<Pager>(p => p
            .Add(c => c.Page, page).Add(c => c.PageCount, count)
            .Add(c => c.PageChanged, EventCallback.Factory.Create<int>(this, pages.Add)));

        var first = Render(1, 3);
        Assert.Equal("Paginação", first.Find("nav").GetAttribute("aria-label"));
        Assert.Contains("Página 1 de 3", first.Markup);
        Assert.True(first.Find("button[data-action='prev']").HasAttribute("disabled"));
        first.Find("button[data-action='next']").Click();

        var last = Render(3, 3);
        Assert.True(last.Find("button[data-action='next']").HasAttribute("disabled"));
        last.Find("button[data-action='prev']").Click();

        Assert.Equal([2, 2], pages);
        Assert.Empty(Render(1, 1).FindAll("button")); // uma página só: sem paginação
    }

    [Fact(DisplayName = "SPEC-0038:E2E-01 — Jornada: resumo, filtro Vitórias, busca, ordenação e página 2")]
    [Trait("Category", "SPEC-0038:E2E-01")]
    public async Task Journey_ShouldSummarizeFilterSearchSortAndPage()
    {
        var (ctx, db) = await RealContext(HistoryData.TwentyFive(DateTime.UtcNow).Take(12).ToList());
        await using var _ = db;
        await using var __ = ctx;

        var cut = ctx.Render<History>();
        WaitRows(cut, 10);
        Assert.Contains("12", cut.Find("[data-stat='total']").TextContent);
        Assert.Contains("Página 1 de 2", cut.Find("nav[aria-label='Paginação']").TextContent);

        Radio(cut, "Filtrar por resultado", "Vitórias").Click();
        WaitRows(cut, 6);
        Assert.Contains("(6)", Radio(cut, "Filtrar por resultado", "Vitórias").TextContent);
        Assert.Empty(cut.FindAll("nav[aria-label='Paginação'] button"));

        cut.Find("input#busca-adversario").Input("rival1");
        WaitRows(cut, 2); // vitórias contra Rival1: partidas 1 e 10

        Radio(cut, "Filtrar por resultado", "Todas").Click();
        WaitRows(cut, 4); // 1, 4, 7 e 10
        Radio(cut, "Ordenar por", "Mais rápidas").Click();
        cut.WaitForAssertion(() => Assert.Equal(["1m 30s", "1m 33s", "1m 36s", "1m 39s"], Cells(cut, "duracao")), TimeSpan.FromSeconds(3));

        cut.Find("input#busca-adversario").Input("");
        Radio(cut, "Ordenar por", "Mais recentes").Click();
        WaitRows(cut, 10);
        cut.Find("nav[aria-label='Paginação'] button[data-action='next']").Click();
        WaitRows(cut, 2);
        Assert.Contains("Página 2 de 2", cut.Find("nav[aria-label='Paginação']").TextContent);
    }
}
