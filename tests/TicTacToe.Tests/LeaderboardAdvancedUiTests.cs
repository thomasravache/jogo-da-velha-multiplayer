using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0039: ranking avançado — interface (pódio, tabela, "Sua posição", paginação)

internal sealed class StubLeaderboardService(GameplayDbContext db) : GameResultService(db, NullLogger<GameResultService>.Instance)
{
    private readonly object _gate = new();
    private readonly List<LeaderboardQuery> _queries = [];

    public Func<LeaderboardQuery, LeaderboardPage> Respond { get; set; } = _ => new LeaderboardPage([], 0, 1, 1, null);

    public List<LeaderboardQuery> Queries
    {
        get { lock (_gate) return [.. _queries]; }
    }

    public override Task<LeaderboardPage> GetLeaderboardPageAsync(LeaderboardQuery query)
    {
        lock (_gate) _queries.Add(query);
        return Task.FromResult(Respond(query));
    }
}

public class LeaderboardAdvancedUiTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    private static LeaderboardEntry Entry(int position, string name, int wins, bool me = false) =>
        new(position, name, wins, 2, 1, 62.5, 3, Now.AddMinutes(-position), me);

    private static LeaderboardPage PageOf(LeaderboardQuery q, int totalPages, LeaderboardEntry? me)
    {
        var page = Math.Clamp(q.Page, 1, totalPages);
        var items = Enumerable.Range((page - 1) * 10 + 1, 10).Select(p => Entry(p, $"Jogador{p}", 100 - p, me is not null && p == me.Position)).ToList();
        return new LeaderboardPage(items, totalPages * 10, page, totalPages, me);
    }

    private static BunitContext NewContext(GameResultService service, InMemoryPlayerStorage? storage = null)
    {
        var ctx = new BunitContext();
        if (storage is null)
        {
            storage = new InMemoryPlayerStorage();
            storage.Data[PlayerIdentityService.StorageKey] = JsonSerializer.Serialize(new { id = HistoryData.Me, nick = "Eu" });
        }

        ctx.Services.AddSingleton<IPlayerStorage>(storage);
        ctx.Services.AddSingleton<PlayerIdentityService>();
        ctx.Services.AddSingleton(service);
        return ctx;
    }

    private static void WaitRows(IRenderedComponent<Leaderboard> cut, int rows) =>
        cut.WaitForAssertion(() => Assert.Equal(rows, cut.FindAll("tbody tr").Count), TimeSpan.FromSeconds(3));

    [Fact(DisplayName = "SPEC-0039:UT-03 — Tabela com oito colunas, 'VOCÊ' na própria linha, pódio com vitórias/aproveitamento/sequência e cartão 'Sua posição'")]
    [Trait("Category", "SPEC-0039:UT-03")]
    public async Task Page_ShouldShowColumnsYouBadgePodiumAndPositionCard()
    {
        await using var db = new GameplayDbContext(HistoryData.NewOptions());
        var me = Entry(3, "Eu", 40, me: true);
        var svc = new StubLeaderboardService(db) { Respond = q => PageOf(q, 1, me) };
        await using var ctx = NewContext(svc);

        var cut = ctx.Render<Leaderboard>();
        WaitRows(cut, 10);

        Assert.Equal(
            ["Posição", "Jogador", "Vitórias", "Derrotas", "Empates", "Aproveitamento", "Sequência", "Último triunfo"],
            cut.FindAll("thead th").Select(t => t.TextContent.Trim()).ToArray());
        Assert.Equal(HistoryData.Me, svc.Queries[0].MyPlayerId);

        var mine = cut.Find("tr[data-me='true']");
        Assert.Contains("VOCÊ", mine.TextContent);
        Assert.Single(cut.FindAll("tr[data-me='true']"));
        Assert.Contains("3º", mine.TextContent);
        var cells = mine.QuerySelectorAll("td").Select(c => c.TextContent).ToArray();
        Assert.Contains("62,5%", string.Join('|', cells));

        var first = cut.Find("[data-podium='1']");
        Assert.Contains("Jogador1", first.TextContent);
        Assert.Contains("99", first.TextContent);
        Assert.Contains("62,5%", first.TextContent);
        Assert.Contains("sequência", first.TextContent, StringComparison.OrdinalIgnoreCase);

        var card = cut.Find("section[aria-label='Sua posição']");
        Assert.Contains("3º", card.TextContent);
        Assert.Contains("62,5%", card.TextContent);
        Assert.Contains("sequência", card.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "SPEC-0039:UT-03b — Cartão: sem vitória mostra o convite; sem identidade fica oculto")]
    [Trait("Category", "SPEC-0039:UT-03")]
    public async Task PositionCard_ShouldCoverNoWinAndNoIdentity()
    {
        await using var db = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new StubLeaderboardService(db) { Respond = q => PageOf(q, 1, null) };

        await using (var withIdentity = NewContext(svc))
        {
            var cut = withIdentity.Render<Leaderboard>();
            WaitRows(cut, 10);
            Assert.Contains("Vença uma partida para entrar no ranking", cut.Find("section[aria-label='Sua posição']").TextContent);
        }

        var broken = new InMemoryPlayerStorage { ThrowException = new NotSupportedException("interop indisponível") };
        await using var noIdentity = NewContext(svc, broken);
        var cutNo = noIdentity.Render<Leaderboard>();
        WaitRows(cutNo, 10);
        Assert.Empty(cutNo.FindAll("section[aria-label='Sua posição']"));
        Assert.Null(svc.Queries[^1].MyPlayerId);
    }

    [Fact(DisplayName = "SPEC-0039:UT-03c — Paginador chama a consulta da página seguinte e o pódio só aparece na primeira")]
    [Trait("Category", "SPEC-0039:UT-03")]
    public async Task Pager_ShouldRequestNextPage_AndPodiumOnlyOnFirst()
    {
        await using var db = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new StubLeaderboardService(db) { Respond = q => PageOf(q, 3, null) };
        await using var ctx = NewContext(svc);

        var cut = ctx.Render<Leaderboard>();
        WaitRows(cut, 10);
        Assert.Equal(3, cut.FindAll("[data-podium]").Count);
        Assert.Contains("Página 1 de 3", cut.Find("nav[aria-label='Paginação']").TextContent);

        cut.WaitForAssertion(() => cut.Find("nav[aria-label='Paginação'] button[data-action='next']").Click(), TimeSpan.FromSeconds(3));
        cut.WaitForAssertion(() => Assert.Equal(2, svc.Queries[^1].Page), TimeSpan.FromSeconds(3));
        cut.WaitForAssertion(() => Assert.Contains("11º", cut.Find("tbody tr").TextContent), TimeSpan.FromSeconds(3));
        Assert.Empty(cut.FindAll("[data-podium]"));
    }

    [Fact(DisplayName = "SPEC-0039:UT-04 — Carregando e vazio da SPEC-0033 permanecem; falha mostra alerta com nova tentativa")]
    [Trait("Category", "SPEC-0039:UT-04")]
    public async Task LoadingEmptyAndError_ShouldRemain()
    {
        await using var db = new GameplayDbContext(HistoryData.NewOptions());
        var svc = new StubLeaderboardService(db);
        await using var ctx = NewContext(svc);

        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Contains("Nenhum jogador pontuou ainda", cut.Markup), TimeSpan.FromSeconds(3));
        Assert.Equal("/", cut.FindAll("a").Single(a => a.TextContent.Contains("Jogar agora")).GetAttribute("href"));

        var calls = 0;
        svc.Respond = q => calls++ == 0 ? throw new InvalidOperationException("boom") : PageOf(q, 1, null);
        await using var ctx2 = NewContext(svc);
        var failing = ctx2.Render<Leaderboard>();
        failing.WaitForAssertion(() => Assert.Contains("Não foi possível carregar o ranking", failing.Markup), TimeSpan.FromSeconds(3));
        failing.WaitForAssertion(() => failing.FindAll("button").Single(b => b.TextContent.Contains("Tentar novamente")).Click(), TimeSpan.FromSeconds(3));
        WaitRows(failing, 10);
    }

    [Fact(DisplayName = "SPEC-0039:E2E-01 — Jornada: pódio, tabela completa, cartão 'Sua posição' e segunda página")]
    [Trait("Category", "SPEC-0039:E2E-01")]
    public async Task Journey_ShouldShowPodiumTableCardAndSecondPage()
    {
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options, LeaderboardData.Players(25, DateTime.UtcNow));
        await using var db = new GameplayDbContext(options);
        var storage = new InMemoryPlayerStorage();
        storage.Data[PlayerIdentityService.StorageKey] = JsonSerializer.Serialize(new { id = LeaderboardData.PlayerId(15), nick = "P15" });
        await using var ctx = NewContext(HistoryData.Service(db), storage);

        var cut = ctx.Render<Leaderboard>();
        WaitRows(cut, 10);

        Assert.Equal(3, cut.FindAll("[data-podium]").Count);
        var top = cut.FindAll("tbody tr")[0].QuerySelectorAll("td").Select(c => c.TextContent.Trim()).ToArray();
        Assert.Contains("P0", string.Join('|', top));
        Assert.Contains("25", string.Join('|', top));
        Assert.Contains("16º", cut.Find("section[aria-label='Sua posição']").TextContent);
        Assert.Empty(cut.FindAll("tr[data-me='true']")); // o jogador está na página 2

        cut.WaitForAssertion(() => cut.Find("nav[aria-label='Paginação'] button[data-action='next']").Click(), TimeSpan.FromSeconds(3));
        WaitRows(cut, 10);
        cut.WaitForAssertion(() => Assert.Contains("11º", cut.FindAll("tbody tr")[0].TextContent), TimeSpan.FromSeconds(3));
        var mine = cut.Find("tr[data-me='true']");
        Assert.Contains("VOCÊ", mine.TextContent);
        Assert.Contains("16º", mine.TextContent);
        Assert.Contains("P15", mine.TextContent);
    }
}
