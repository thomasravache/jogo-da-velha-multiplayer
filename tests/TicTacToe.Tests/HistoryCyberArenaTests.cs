using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0032: Histórico de partidas Cyber Arena

public class HistoryCyberArenaTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string HistoryRazor = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Components/Pages/History.razor");

    private static DbContextOptions<GameplayDbContext> NewOptions() =>
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static async Task Seed(DbContextOptions<GameplayDbContext> options, params MatchResult[] results)
    {
        await using var db = new GameplayDbContext(options);
        db.MatchResults.AddRange(results);
        await db.SaveChangesAsync();
    }

    private static MatchResult Match(string x, string o, string? winner, DateTime playedAtUtc) =>
        new() { PlayerXName = x, PlayerOName = o, WinnerName = winner, PlayedAt = playedAtUtc };

    private static BunitContext NewContext(DbContextOptions<GameplayDbContext> options)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IPlayerStorage>(new InMemoryPlayerStorage());
        ctx.Services.AddSingleton<PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return ctx;
    }

    // A página abre no escopo pessoal (SPEC-0038); estes testes cobrem o escopo global da SPEC-0032.
    private static IRenderedComponent<History> RenderAll(BunitContext ctx)
    {
        var cut = RenderAll(ctx);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[role='radiogroup'][aria-label='Escopo']")));
        cut.FindAll("[role='radiogroup'][aria-label='Escopo'] [role='radio']").Single(r => r.TextContent.Trim() == "Todos").Click();
        return cut;
    }

    [Fact(DisplayName = "SPEC-0032:CH-01 — GetRecentAsync(10) devolve as 10 mais recentes em ordem decrescente")]
    [Trait("Category", "SPEC-0032:CH-01")]
    public async Task GetRecentAsync_ShouldReturnTenNewestDescending()
    {
        var options = NewOptions();
        var now = DateTime.UtcNow;
        await Seed(options, Enumerable.Range(0, 11).Select(i => Match($"X{i}", $"O{i}", $"X{i}", now.AddMinutes(-i))).ToArray());

        await using var db = new GameplayDbContext(options);
        var results = await new GameResultService(db, NullLogger<GameResultService>.Instance).GetRecentAsync(10);

        Assert.Equal(10, results.Count);
        Assert.Equal("X0", results[0].PlayerXName);
        Assert.Equal("X9", results[9].PlayerXName);
    }

    [Fact(DisplayName = "SPEC-0032:UT-01 — Rótulo de data: hoje, ontem e dias anteriores em fuso local")]
    [Trait("Category", "SPEC-0032:UT-01")]
    public void DateFormatter_ShouldLabelTodayYesterdayAndOlder()
    {
        var now = new DateTime(2026, 9, 29, 20, 0, 0, DateTimeKind.Local);
        static DateTime Utc(int y, int m, int d, int h, int min) =>
            new DateTime(y, m, d, h, min, 0, DateTimeKind.Local).ToUniversalTime();

        Assert.Equal("Hoje, 19:42", HistoryDateFormatter.Format(Utc(2026, 9, 29, 19, 42), now));
        Assert.Equal("Ontem, 22:10", HistoryDateFormatter.Format(Utc(2026, 9, 28, 22, 10), now));
        Assert.Equal("26/09 18:30", HistoryDateFormatter.Format(Utc(2026, 9, 26, 18, 30), now));
        Assert.Equal("Ontem, 23:59", HistoryDateFormatter.Format(Utc(2026, 9, 28, 23, 59), new DateTime(2026, 9, 29, 0, 1, 0, DateTimeKind.Local)));
    }

    [Fact(DisplayName = "SPEC-0032:UT-02 — Vitória mostra 'Vitória de {nome}' com vencedor destacado; empate mostra 'Deu velha'")]
    [Trait("Category", "SPEC-0032:UT-02")]
    public async Task Rows_ShouldShowWinnerAndDraw()
    {
        var options = NewOptions();
        var now = DateTime.UtcNow;
        await Seed(options, Match("Thomas", "Ana", "Ana", now), Match("Bia", "Caio", null, now.AddMinutes(-5)));
        await using var ctx = NewContext(options);

        var cut = RenderAll(ctx);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tbody tr").Count));
        var rows = cut.FindAll("tbody tr");

        Assert.Contains("Vitória de Ana", rows[0].TextContent);
        Assert.Contains("Ana", rows[0].QuerySelector("[data-winner='true']")!.TextContent);
        Assert.DoesNotContain("Thomas", rows[0].QuerySelector("[data-winner='true']")!.TextContent);
        Assert.Contains("Deu velha", rows[1].TextContent);
        Assert.Empty(rows[1].QuerySelectorAll("[data-winner='true']"));
    }

    [Fact(DisplayName = "SPEC-0032:UT-03 — Homônimos com vencedor: chip cita o nome e nenhum lado é destacado")]
    [Trait("Category", "SPEC-0032:UT-03")]
    public async Task SameNameOnBothSides_ShouldNotHighlightASide()
    {
        var options = NewOptions();
        await Seed(options, Match("Thomas", "Thomas", "Thomas", DateTime.UtcNow));
        await using var ctx = NewContext(options);

        var cut = RenderAll(ctx);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("tbody tr")));

        Assert.Contains("Vitória de Thomas", cut.Find("tbody tr").TextContent);
        Assert.Empty(cut.FindAll("[data-winner='true']"));
    }

    [Fact(DisplayName = "SPEC-0032:UT-04 — Estado de carregamento acessível e estado vazio com 'Jogar agora'")]
    [Trait("Category", "SPEC-0032:UT-04")]
    public async Task LoadingAndEmptyStates_ShouldBeAccessible()
    {
        var source = File.ReadAllText(HistoryRazor);
        Assert.Contains("role=\"status\"", source);
        Assert.Contains("Carregando", source);

        await using var ctx = NewContext(NewOptions());
        var cut = RenderAll(ctx);
        cut.WaitForAssertion(() => Assert.Contains("Nenhuma partida registrada ainda", cut.Markup));

        var cta = cut.FindAll("a").Single(a => a.TextContent.Contains("Jogar agora"));
        Assert.Equal("/", cta.GetAttribute("href"));
    }

    [Fact(DisplayName = "SPEC-0032:UT-05 — Cabeçalho com contagem de partidas e ação 'Novo duelo'")]
    [Trait("Category", "SPEC-0032:UT-05")]
    public async Task Header_ShouldShowCountAndNewDuelAction()
    {
        var options = NewOptions();
        var now = DateTime.UtcNow;
        await Seed(options, Enumerable.Range(0, 3).Select(i => Match($"X{i}", $"O{i}", $"X{i}", now.AddMinutes(-i))).ToArray());
        await using var ctx = NewContext(options);

        var cut = RenderAll(ctx);
        cut.WaitForAssertion(() => Assert.Contains("3 partidas", cut.Markup));

        var action = cut.FindAll("a").Single(a => a.TextContent.Contains("Novo duelo"));
        Assert.Equal("/", action.GetAttribute("href"));
    }

    [Fact(DisplayName = "SPEC-0032:UT-06 — Sem MudBlazor, sem <style> inline e com título da aba")]
    [Trait("Category", "SPEC-0032:UT-06")]
    public async Task Page_ShouldNotUseLegacyStyling()
    {
        var source = File.ReadAllText(HistoryRazor);
        Assert.DoesNotContain("<style", source);
        Assert.Contains("<PageTitle>Histórico de Partidas · XO Arena</PageTitle>", source);

        var options = NewOptions();
        await Seed(options, Match("A", "B", "A", DateTime.UtcNow));
        await using var ctx = NewContext(options);
        var cut = RenderAll(ctx);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("tbody tr")));
        Assert.DoesNotContain("mud-", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0032:IT-01 — Com 11 partidas a página mostra só as 10 mais recentes")]
    [Trait("Category", "SPEC-0032:IT-01")]
    public async Task Page_ShouldShowOnlyTenNewest()
    {
        var options = NewOptions();
        var now = DateTime.UtcNow;
        await Seed(options, Enumerable.Range(0, 11).Select(i => Match($"X{i}", $"O{i}", $"X{i}", now.AddMinutes(-i))).ToArray());
        await using var ctx = NewContext(options);

        var cut = RenderAll(ctx);
        cut.WaitForAssertion(() => Assert.Equal(10, cut.FindAll("tbody tr").Count));

        Assert.Contains("X0", cut.FindAll("tbody tr")[0].TextContent);
        Assert.DoesNotContain("X10", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0032:E2E-01 — Jornada do histórico: linhas com chip, duelo e data e ação Novo duelo")]
    [Trait("Category", "SPEC-0032:E2E-01")]
    public async Task HistoryJourney_ShouldListMatchesWithChipDuelAndDate()
    {
        var options = NewOptions();
        var now = DateTime.UtcNow;
        await Seed(options, Match("Thomas", "Ana", "Thomas", now), Match("Bia", "Caio", null, now.AddDays(-1)));
        await using var ctx = NewContext(options);

        var cut = RenderAll(ctx);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tbody tr").Count));

        var first = cut.FindAll("tbody tr")[0].TextContent;
        Assert.Contains("Vitória de Thomas", first);
        Assert.Contains("Ana", first);
        Assert.Contains("Hoje,", first);
        Assert.Contains("Ontem,", cut.FindAll("tbody tr")[1].TextContent);
        Assert.NotEmpty(cut.FindAll("th"));
        Assert.Contains(cut.FindAll("a"), a => a.TextContent.Contains("Novo duelo"));
    }

    [Fact(DisplayName = "SPEC-0032:UT-01b — Formatador trata datas com Kind Unspecified como UTC (retorno do EF)")]
    [Trait("Category", "SPEC-0032:UT-01")]
    public void DateFormatter_ShouldTreatUnspecifiedKindAsUtc()
    {
        var local = new DateTime(2026, 9, 29, 19, 42, 0, DateTimeKind.Local);
        var unspecified = DateTime.SpecifyKind(local.ToUniversalTime(), DateTimeKind.Unspecified);

        Assert.Equal("Hoje, 19:42", HistoryDateFormatter.Format(unspecified, new DateTime(2026, 9, 29, 20, 0, 0, DateTimeKind.Local)));
    }

    [Fact(DisplayName = "SPEC-0032:UT-05b — Subtítulo: oculto sem partidas e singular com uma")]
    [Trait("Category", "SPEC-0032:UT-05")]
    public async Task Subtitle_ShouldHandleZeroAndOne()
    {
        var empty = NewOptions();
        await using var ctxEmpty = NewContext(empty);
        var none = RenderAll(ctxEmpty);
        none.WaitForAssertion(() => Assert.Contains("Nenhuma partida registrada ainda", none.Markup));
        Assert.DoesNotContain("0 partidas", none.Markup);

        var one = NewOptions();
        await Seed(one, Match("A", "B", "A", DateTime.UtcNow));
        await using var ctxOne = NewContext(one);
        var single = RenderAll(ctxOne);
        single.WaitForAssertion(() => Assert.Single(single.FindAll("tbody tr")));
        Assert.Contains("1 partida", single.Markup);
        Assert.DoesNotContain("1 partidas", single.Markup);
    }
}
