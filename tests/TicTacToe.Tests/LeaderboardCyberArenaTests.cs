using System;
using System.Globalization;
using System.IO;
using System.Linq;
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

// SPEC-0033: Ranking Cyber Arena

public class LeaderboardCyberArenaTests
{
    private static readonly string[] PodiumFull = ["2", "1", "3"];
    private static readonly string[] PodiumTwo = ["2", "1"];
    private static readonly string[] TieOrder = ["Recente", "Antiga"];
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string LeaderboardRazor = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Components/Pages/Leaderboard.razor");

    private static DbContextOptions<GameplayDbContext> NewOptions() =>
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    // n vitórias do jogador `name`, com a última em `lastWinUtc`.
    private static MatchResult[] Wins(string name, int count, DateTime lastWinUtc) =>
        Enumerable.Range(0, count)
            .Select(i => new MatchResult { PlayerXName = name, PlayerOName = "Rival", WinnerName = name, PlayedAt = lastWinUtc.AddMinutes(-i) })
            .ToArray();

    private static async Task Seed(DbContextOptions<GameplayDbContext> options, params MatchResult[] results)
    {
        await using var db = new GameplayDbContext(options);
        db.MatchResults.AddRange(results);
        await db.SaveChangesAsync();
    }

    private static BunitContext NewContext(DbContextOptions<GameplayDbContext> options)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IPlayerStorage>(new InMemoryPlayerStorage());
        ctx.Services.AddSingleton<PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return ctx;
    }

    private static async Task<DbContextOptions<GameplayDbContext>> SeedPlayers(int players)
    {
        var options = NewOptions();
        var now = DateTime.UtcNow;
        // Jogador i tem (players - i) vitórias: 1º é o jogador P0.
        await Seed(options, Enumerable.Range(0, players).SelectMany(i => Wins($"P{i}", players - i, now.AddHours(-i))).ToArray());
        return options;
    }

    [Fact(DisplayName = "SPEC-0033:CH-01 — Leaderboard desempata por último triunfo e ignora empates")]
    [Trait("Category", "SPEC-0033:CH-01")]
    public async Task GetLeaderboardAsync_ShouldTieBreakByLastWinAndIgnoreDraws()
    {
        var options = NewOptions();
        var now = DateTime.UtcNow;
        await Seed(options,
            Wins("Antiga", 2, now.AddDays(-2)).Concat(Wins("Recente", 2, now)).Concat(
                [new MatchResult { PlayerXName = "A", PlayerOName = "B", WinnerName = null, PlayedAt = now }]).ToArray());

        await using var db = new GameplayDbContext(options);
        var ranks = await new GameResultService(db, NullLogger<GameResultService>.Instance).GetLeaderboardAsync(10);

        Assert.Equal(TieOrder, ranks.Select(r => r.PlayerName).ToArray());
    }

    [Fact(DisplayName = "SPEC-0033:UT-01 — Pódio com 3 cartões na ordem 2º, 1º, 3º e ouro para o 1º")]
    [Trait("Category", "SPEC-0033:UT-01")]
    public async Task Podium_ShouldRenderThreeCardsInVisualOrder()
    {
        await using var ctx = NewContext(await SeedPlayers(4));
        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-podium]").Count));

        var cards = cut.FindAll("[data-podium]");
        Assert.Equal(PodiumFull, cards.Select(c => c.GetAttribute("data-podium")).ToArray());
        Assert.Contains("min-h-56", cards[1].GetAttribute("class"));
        Assert.Contains("border-gold", cards[1].GetAttribute("class"));
        Assert.DoesNotContain("min-h-56", cards[0].GetAttribute("class"));
        Assert.Contains("1º", cards[1].TextContent);
        Assert.Contains("P0", cards[1].TextContent);
        Assert.Contains("4", cards[1].TextContent);
        Assert.Contains("2º", cards[0].TextContent);
        Assert.Contains("3º", cards[2].TextContent);
    }

    [Fact(DisplayName = "SPEC-0033:UT-02 — Pódio parcial com 1 ou 2 jogadores, sem cartões vazios")]
    [Trait("Category", "SPEC-0033:UT-02")]
    public async Task Podium_ShouldRenderOnlyExistingPlayers()
    {
        await using var one = NewContext(await SeedPlayers(1));
        var cutOne = one.Render<Leaderboard>();
        cutOne.WaitForAssertion(() => Assert.Single(cutOne.FindAll("[data-podium]")));

        await using var two = NewContext(await SeedPlayers(2));
        var cutTwo = two.Render<Leaderboard>();
        cutTwo.WaitForAssertion(() => Assert.Equal(2, cutTwo.FindAll("[data-podium]").Count));
        Assert.Equal(PodiumTwo, cutTwo.FindAll("[data-podium]").Select(c => c.GetAttribute("data-podium")).ToArray());
    }

    [Fact(DisplayName = "SPEC-0033:UT-03 — Tabela com posição, jogador, vitórias e último triunfo; 1 a 3 em ouro")]
    [Trait("Category", "SPEC-0033:UT-03")]
    public async Task Table_ShouldListRanksWithGoldTopThree()
    {
        var options = await SeedPlayers(5);
        var expectedDate = DateTime.SpecifyKind(
            (await new GameplayDbContext(options).MatchResults.Where(m => m.WinnerName == "P0").MaxAsync(m => m.PlayedAt)), DateTimeKind.Utc)
            .ToLocalTime().ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);
        await using var ctx = NewContext(options);

        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("tbody tr").Count));
        var rows = cut.FindAll("tbody tr");

        Assert.Contains("1", rows[0].TextContent);
        Assert.Contains("P0", rows[0].TextContent);
        Assert.Contains("5", rows[0].TextContent);
        Assert.Contains(expectedDate, rows[0].TextContent);
        Assert.Contains("P4", rows[4].TextContent);
        foreach (var i in new[] { 0, 1, 2 }) Assert.Contains("bg-gold/10", rows[i].GetAttribute("class"));
        foreach (var i in new[] { 3, 4 }) Assert.DoesNotContain("bg-gold/10", rows[i].GetAttribute("class"));
        Assert.NotEmpty(cut.FindAll("th[scope='col']"));
    }

    [Fact(DisplayName = "SPEC-0033:UT-04 — Carregando acessível e estado vazio com 'Jogar agora'")]
    [Trait("Category", "SPEC-0033:UT-04")]
    public async Task LoadingAndEmptyStates_ShouldBeAccessible()
    {
        var source = File.ReadAllText(LeaderboardRazor);
        Assert.Contains("role=\"status\"", source);
        Assert.Contains("Carregando", source);

        await using var ctx = NewContext(NewOptions());
        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Contains("Nenhum jogador pontuou ainda", cut.Markup));

        var cta = cut.FindAll("a").Single(a => a.TextContent.Contains("Jogar agora"));
        Assert.Equal("/", cta.GetAttribute("href"));
    }

    [Fact(DisplayName = "SPEC-0033:UT-05 — Sem MudBlazor, sem <style> inline e com título da aba")]
    [Trait("Category", "SPEC-0033:UT-05")]
    public async Task Page_ShouldNotUseLegacyStyling()
    {
        var source = File.ReadAllText(LeaderboardRazor);
        Assert.DoesNotContain("<style", source);
        Assert.Contains("<PageTitle>Ranking · XO Arena</PageTitle>", source);

        await using var ctx = NewContext(await SeedPlayers(3));
        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("tbody tr").Count));
        Assert.DoesNotContain("mud-", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0033:IT-01 — Ordem por vitórias com desempate e limite de 10 linhas")]
    [Trait("Category", "SPEC-0033:IT-01")]
    public async Task Page_ShouldRespectOrderTieBreakAndLimit()
    {
        var options = NewOptions();
        var now = DateTime.UtcNow;
        await Seed(options,
            Wins("Empate-Antigo", 3, now.AddDays(-1)).Concat(Wins("Empate-Recente", 3, now))
                .Concat(Enumerable.Range(0, 10).SelectMany(i => Wins($"Q{i}", 1, now.AddHours(-i)))).ToArray());
        await using var ctx = NewContext(options);

        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Equal(10, cut.FindAll("tbody tr").Count));
        var rows = cut.FindAll("tbody tr");

        Assert.Contains("Empate-Recente", rows[0].TextContent);
        Assert.Contains("Empate-Antigo", rows[1].TextContent);
    }

    [Fact(DisplayName = "SPEC-0033:E2E-01 — Jornada do ranking: pódio, tabela e ação Jogar agora")]
    [Trait("Category", "SPEC-0033:E2E-01")]
    public async Task LeaderboardJourney_ShouldShowPodiumTableAndAction()
    {
        await using var ctx = NewContext(await SeedPlayers(4));
        var cut = ctx.Render<Leaderboard>();
        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("tbody tr").Count));

        Assert.Equal(3, cut.FindAll("[data-podium]").Count);
        Assert.Contains(cut.FindAll("a"), a => a.TextContent.Contains("Jogar agora") && a.GetAttribute("href") == "/");
    }
}
