using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Layout;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0048: seleção de jogos e navegação por jogo

public class GameSelectTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    private static string[] Routes(Type page) =>
        [.. page.GetCustomAttributes<RouteAttribute>(false).Select(r => r.Template)];

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ShellState());
        ctx.Services.AddSingleton<MatchmakingService>();
        ctx.Services.AddSingleton<ConcurrentDictionary<Guid, GameSession>>();
        ctx.Services.AddSingleton<IPlayerStorage>(new InMemoryPlayerStorage());
        ctx.Services.AddScoped<PlayerIdentityService>();
        var options = new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return ctx;
    }

    [Fact(DisplayName = "SPEC-0048:UT-01 — GameSelect mostra duas cartas com título, descrição, chip Online e links")]
    [Trait("Category", "SPEC-0048:UT-01")]
    public async Task GameSelect_ShouldRenderTwoCards()
    {
        await using var ctx = NewContext();
        var cut = ctx.Render<GameSelect>();

        var cards = cut.FindAll("a");
        Assert.Equal(2, cards.Count);
        Assert.Equal("/velha", cards[0].GetAttribute("href"));
        Assert.Equal("/xadrez", cards[1].GetAttribute("href"));
        Assert.Contains("Jogo da Velha", cards[0].QuerySelector("h2")!.TextContent);
        Assert.Contains("Xadrez", cards[1].QuerySelector("h2")!.TextContent);
        foreach (var card in cards)
        {
            Assert.False(string.IsNullOrWhiteSpace(card.QuerySelector("p")?.TextContent), "descrição curta");
            Assert.Contains("Online", card.TextContent);
            Assert.Contains("Jogar", card.TextContent);
        }
    }

    [Fact(DisplayName = "SPEC-0048:UT-02 — Home responde em /velha e GameSelect em /")]
    [Trait("Category", "SPEC-0048:UT-02")]
    public void Routes_ShouldPlaceHomeOnVelhaAndSelectionOnRoot()
    {
        Assert.Equal(["/velha"], Routes(typeof(Home)));
        Assert.Equal(["/"], Routes(typeof(GameSelect)));

        var razor = File.ReadAllText(Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Components/Pages/GameSelect.razor"));
        Assert.Contains("<PageTitle>Escolha seu jogo · XO Arena</PageTitle>", razor, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0048:UT-04 — Cartas são links com rótulo acessível, ordem Velha → Xadrez e sem style inline")]
    [Trait("Category", "SPEC-0048:UT-04")]
    public async Task Cards_ShouldBeLabelledLinksInLogicalOrder()
    {
        await using var ctx = NewContext();
        var cut = ctx.Render<GameSelect>();

        var links = cut.FindAll("a");
        Assert.Equal(["Jogar Jogo da Velha", "Jogar Xadrez"], links.Select(a => a.GetAttribute("aria-label")!).ToArray());
        Assert.Equal(["/velha", "/xadrez"], links.Select(a => a.GetAttribute("href")!).ToArray());
        Assert.All(links, a => Assert.Null(a.GetAttribute("tabindex")));
        Assert.Empty(cut.FindAll("style"));
        Assert.DoesNotContain("<style", cut.Markup, StringComparison.OrdinalIgnoreCase);

        var razor = File.ReadAllText(Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Components/Pages/GameSelect.razor"));
        Assert.DoesNotContain("<style", razor, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "SPEC-0048:IT-01 — Layout com GameSelect: ativar o link da Velha navega a /velha e 'Jogar' segue ativo")]
    [Trait("Category", "SPEC-0048:IT-01")]
    public async Task Layout_ShouldKeepPlayActive_AfterFollowingVelhaLink()
    {
        await using var ctx = NewContext();
        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b =>
        {
            b.OpenComponent<GameSelect>(0);
            b.CloseComponent();
        })));

        var href = cut.Find("main a[aria-label='Jogar Jogo da Velha']").GetAttribute("href")!;
        nav.NavigateTo(href);

        Assert.EndsWith("/velha", nav.Uri, StringComparison.Ordinal);
        cut.WaitForAssertion(() =>
        {
            var current = cut.FindAll("header nav a[aria-current='page']");
            Assert.Single(current);
            Assert.Equal("Jogar", current[0].TextContent.Trim());
        });
    }

    [Fact(DisplayName = "SPEC-0048:E2E-01 — Jornada: seleção, seguir o link da Velha e encontrar o lobby")]
    [Trait("Category", "SPEC-0048:E2E-01")]
    public async Task Journey_ShouldGoFromSelectionToVelhaLobby()
    {
        await using var ctx = NewContext();
        var nav = ctx.Services.GetRequiredService<NavigationManager>();

        var select = ctx.Render<GameSelect>();
        Assert.Equal(2, select.FindAll("a").Count);
        nav.NavigateTo(select.Find("a[aria-label='Jogar Jogo da Velha']").GetAttribute("href")!);
        Assert.EndsWith("/velha", nav.Uri, StringComparison.Ordinal);

        var home = ctx.Render<Home>();
        Assert.Contains("Procurar oponente", home.Markup);
    }

    [Fact(DisplayName = "SPEC-0048:CH-01 — Home renderizado diretamente mantém lobby e nenhum conteúdo da seleção")]
    [Trait("Category", "SPEC-0048:CH-01")]
    public async Task Home_ShouldStillRenderLobby()
    {
        await using var ctx = NewContext();
        var home = ctx.Render<Home>();

        Assert.Contains("Procurar oponente", home.Markup);
        Assert.NotEmpty(home.FindAll("input#playerName"));
        Assert.Empty(home.FindAll("a[aria-label='Jogar Xadrez']"));
    }
}
