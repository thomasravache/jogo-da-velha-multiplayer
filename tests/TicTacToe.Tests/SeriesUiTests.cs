using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Layout;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0044: série melhor de 5 — interface no lobby e na arena

public class SeriesUiTests
{
    private static GameSession NewSeries(string x = "Ana", string o = "Bia")
    {
        var g = new GameSession(enableBackgroundTimer: false, format: SeriesFormat.BestOf5);
        g.SetPlayerName(Player.X, x);
        g.SetPlayerName(Player.O, o);
        return g;
    }

    private static string ButtonLabel(RematchBar.RematchKind kind)
    {
        using var ctx = new BunitContext();
        return ctx.Render<RematchBar>(p => p.Add(r => r.Finished, true).Add(r => r.Kind, kind)).Find("button").TextContent.Trim();
    }

    [Fact(DisplayName = "SPEC-0044:CH-01 — Em partida única a arena não ganha nenhum elemento de série")]
    [Trait("Category", "SPEC-0044:CH-01")]
    public void Single_ArenaMarkupHasNoSeriesElements()
    {
        using var g = new GameSession(enableBackgroundTimer: false);
        SeriesRulesTests.WinRound(g, Player.X);
        using var ctx = new BunitContext();

        var markup = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, Player.X)).Markup;

        Assert.Contains("data-player=\"X\"", markup);
        Assert.Contains("data-score", markup);
        Assert.DoesNotContain("data-series", markup);
        Assert.DoesNotContain("Match point", markup);
        Assert.Equal("Jogar novamente", ButtonLabel(RematchBar.RematchKind.Rematch));
    }

    [Fact(DisplayName = "SPEC-0044:UT-01 — Lobby: seletor de formato marca o atual, dispara FormatChanged e mostra a descrição")]
    [Trait("Category", "SPEC-0044:UT-01")]
    public void Lobby_ShouldOfferFormatSelector()
    {
        using var ctx = new BunitContext();
        SeriesFormat? received = null;
        var cut = ctx.Render<Lobby>(p => p
            .Add(l => l.SelectedFormat, SeriesFormat.Single)
            .Add(l => l.FormatChanged, EventCallback.Factory.Create<SeriesFormat>(this, f => received = f)));

        var group = cut.Find("[role='radiogroup'][aria-label='Formato']");
        var radios = group.QuerySelectorAll("[role='radio']").ToList();
        Assert.Equal(["Partida única", "Melhor de 5"], radios.Select(r => r.TextContent.Trim()).ToArray());
        Assert.Equal("true", radios[0].GetAttribute("aria-checked"));
        Assert.DoesNotContain("Vence quem chegar a 3 rodadas", cut.Markup);

        radios[1].Click();

        Assert.Equal(SeriesFormat.BestOf5, received);
        Assert.Contains("Vence quem chegar a 3 rodadas", cut.Markup);
        Assert.Equal("true", cut.Find("[role='radiogroup'][aria-label='Formato']").QuerySelectorAll("[role='radio']")[1].GetAttribute("aria-checked"));
    }

    [Fact(DisplayName = "SPEC-0044:UT-02 — PlayerCard: marcadores de vitória, texto alternativo e chip Match point")]
    [Trait("Category", "SPEC-0044:UT-02")]
    public void PlayerCard_ShouldShowSeriesMarkersAndMatchPoint()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<PlayerCard>(p => p
            .Add(c => c.Player, Player.X).Add(c => c.Name, "Ana")
            .Add(c => c.SeriesWins, 2).Add(c => c.SeriesTarget, 3).Add(c => c.MatchPoint, true));

        var markers = cut.FindAll("[data-series-marker]");
        Assert.Equal(3, markers.Count);
        Assert.Equal([true, true, false], markers.Select(m => m.GetAttribute("data-filled") == "true").ToArray());
        Assert.NotNull(cut.Find("[aria-label='2 de 3 vitórias']"));
        Assert.Contains("Match point", cut.Markup);

        var plain = ctx.Render<PlayerCard>(p => p.Add(c => c.Player, Player.X).Add(c => c.Name, "Ana").Add(c => c.SeriesTarget, 0));
        Assert.Empty(plain.FindAll("[data-series-marker]"));
        Assert.DoesNotContain("Match point", plain.Markup);
    }

    [Fact(DisplayName = "SPEC-0044:UT-03 — Cabeçalho 'Melhor de 5 • Rodada N de 5' só em série e nos cards marcadores e match point")]
    [Trait("Category", "SPEC-0044:UT-03")]
    public void Scoreboard_ShouldShowRoundHeaderOnlyInSeries()
    {
        using var g = NewSeries();
        SeriesRulesTests.WinRound(g, Player.X);
        g.Restart();
        SeriesRulesTests.WinRound(g, Player.O);
        g.Restart(); // rodada 3 em andamento
        using var ctx = new BunitContext();

        var cut = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, Player.X));

        Assert.Equal("Melhor de 5 • Rodada 3 de 5", cut.Find("[data-series-header]").TextContent.Trim());
        Assert.Equal(6, cut.FindAll("[data-series-marker]").Count);
        Assert.Empty(cut.FindAll("[data-series-notice]")); // rodada em andamento: sem aviso

        using var single = new GameSession(enableBackgroundTimer: false);
        var plain = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, single).Add(s => s.MyPlayer, Player.X));
        Assert.Empty(plain.FindAll("[data-series-header]"));
    }

    [Fact(DisplayName = "SPEC-0044:UT-03b — Match point aparece no jogador com 2 vitórias")]
    [Trait("Category", "SPEC-0044:UT-03")]
    public void Scoreboard_ShouldShowMatchPointForLeader()
    {
        using var g = NewSeries();
        SeriesRulesTests.WinRound(g, Player.X);
        g.Restart();
        SeriesRulesTests.WinRound(g, Player.X);
        g.Restart();
        using var ctx = new BunitContext();

        var cut = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, Player.X));

        Assert.Contains("Match point", cut.Find("[data-player='X']").TextContent);
        Assert.DoesNotContain("Match point", cut.Find("[data-player='O']").TextContent);
    }

    [Theory(DisplayName = "SPEC-0044:UT-04 — RematchBar: rótulo por tipo e clique dispara OnRematch")]
    [Trait("Category", "SPEC-0044:UT-04")]
    [InlineData(RematchBar.RematchKind.Rematch, "Jogar novamente")]
    [InlineData(RematchBar.RematchKind.NextRound, "Próxima rodada")]
    [InlineData(RematchBar.RematchKind.NewSeries, "Nova série")]
    public void RematchBar_ShouldLabelByKind(RematchBar.RematchKind kind, string label)
    {
        var clicks = 0;
        using var ctx = new BunitContext();
        var cut = ctx.Render<RematchBar>(p => p
            .Add(r => r.Finished, true).Add(r => r.Kind, kind)
            .Add(r => r.OnRematch, EventCallback.Factory.Create(this, () => clicks++)));

        Assert.Equal(label, cut.Find("button").TextContent.Trim());
        cut.Find("button").Click();
        Assert.Equal(1, clicks);
    }

    [Fact(DisplayName = "SPEC-0044:UT-05 — Avisos de rodada, empate e série encerrada em região aria-live")]
    [Trait("Category", "SPEC-0044:UT-05")]
    public void Scoreboard_ShouldAnnounceRoundDrawAndSeriesEnd()
    {
        using var ctx = new BunitContext();
        IRenderedComponent<Scoreboard> Render(GameSession g) => ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, Player.X));

        using var round = NewSeries();
        SeriesRulesTests.WinRound(round, Player.X);
        var notice = Render(round).Find("[data-series-notice]");
        Assert.Equal("polite", notice.GetAttribute("aria-live"));
        Assert.Contains("Rodada 1 para Ana!", notice.TextContent);

        using var draw = NewSeries();
        SeriesRulesTests.DrawRound(draw);
        Assert.Contains("Rodada empatada, será repetida", Render(draw).Find("[data-series-notice]").TextContent);

        using var over = NewSeries();
        SeriesRulesTests.WinRound(over, Player.X);
        over.Restart();
        SeriesRulesTests.WinRound(over, Player.O);
        over.Restart();
        SeriesRulesTests.WinRound(over, Player.X);
        over.Restart();
        SeriesRulesTests.WinRound(over, Player.X);
        Assert.True(over.IsSeriesOver);
        Assert.Contains("Ana venceu a série 3 × 1", Render(over).Find("[data-series-notice]").TextContent);
    }

    [Fact(DisplayName = "SPEC-0044:UT-06 — Partida única não renderiza marcador, chip, cabeçalho nem aviso de série")]
    [Trait("Category", "SPEC-0044:UT-06")]
    public void Single_ShouldRenderNoSeriesUi()
    {
        using var g = new GameSession(enableBackgroundTimer: false);
        SeriesRulesTests.DrawRound(g);
        using var ctx = new BunitContext();

        var cut = ctx.Render<Scoreboard>(p => p.Add(s => s.Game, g).Add(s => s.MyPlayer, Player.X));

        Assert.Empty(cut.FindAll("[data-series-marker]"));
        Assert.Empty(cut.FindAll("[data-series-header]"));
        Assert.Empty(cut.FindAll("[data-series-notice]"));
        Assert.DoesNotContain("Match point", cut.Markup);
    }

    private static BunitContext NewHomeContext(out MatchmakingService mm, out ConcurrentDictionary<Guid, GameSession> games)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        mm = new MatchmakingService();
        games = new ConcurrentDictionary<Guid, GameSession>();
        var options = new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        ctx.Services.AddSingleton(mm);
        ctx.Services.AddSingleton(games);
        ctx.Services.AddSingleton(new ShellState());
        ctx.Services.AddSingleton<IPlayerStorage>(new InMemoryPlayerStorage());
        ctx.Services.AddScoped<PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return ctx;
    }

    private static void ChooseBestOf5(IRenderedComponent<Home> home)
    {
        home.WaitForAssertion(() => home.FindAll("[role='radiogroup'][aria-label='Formato'] [role='radio']").Single(r => r.TextContent.Contains("Melhor de 5")).Click());
        home.WaitForAssertion(() => Assert.Equal("true", home.FindAll("[role='radiogroup'][aria-label='Formato'] [role='radio']")[1].GetAttribute("aria-checked")));
    }

    private static void TypeName(IRenderedComponent<Home> home) =>
        home.WaitForAssertion(() => home.Find("input#playerName").Input("Ana"));

    [Fact(DisplayName = "SPEC-0044:IT-01 — Home leva o formato escolhido ao solo, à fila e à sala privada")]
    [Trait("Category", "SPEC-0044:IT-01")]
    public async Task Home_ShouldCarryChosenFormatToDomain()
    {
        // Solo
        await using (var ctx = NewHomeContext(out _, out var games))
        {
            var home = ctx.Render<Home>();
            TypeName(home);
            ChooseBestOf5(home);
            home.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Iniciar partida solo")).Click();
            Assert.Equal(SeriesFormat.BestOf5, Assert.Single(games.Values).Format);
        }

        // Fila: outro jogador entra com bestOf 5 e forma a partida com o Home
        await using (var ctx = NewHomeContext(out var mm, out var games))
        {
            var home = ctx.Render<Home>();
            TypeName(home);
            ChooseBestOf5(home);
            home.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Procurar oponente")).Click();

            var matchId = mm.JoinQueue("outro", "Bia", bestOf: 5);
            Assert.NotNull(matchId);
            Assert.Equal(5, mm.GetMatchBestOf(matchId!.Value));
            home.WaitForAssertion(() => Assert.Equal(SeriesFormat.BestOf5, Assert.Single(games.Values).Format));
        }

        // Sala privada
        await using (var ctx = NewHomeContext(out var mm, out var games))
        {
            var home = ctx.Render<Home>();
            TypeName(home);
            ChooseBestOf5(home);
            home.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Criar sala")).Click();
            var code = home.WaitForElement("[aria-label='Código da sala']").TextContent.Trim();

            var matchId = mm.JoinPrivateRoom(code, "amigo", "Bia");
            Assert.NotNull(matchId);
            Assert.Equal(5, mm.GetMatchBestOf(matchId!.Value));
            home.WaitForAssertion(() => Assert.Equal(SeriesFormat.BestOf5, Assert.Single(games.Values).Format));
        }
    }

    [Fact(DisplayName = "SPEC-0044:E2E-01 — Jornada da série: escolher Melhor de 5, iniciar solo, ver rodada 1 e jogar")]
    [Trait("Category", "SPEC-0044:E2E-01")]
    public async Task Journey_ShouldStartSeriesAndPlayFirstMove()
    {
        await using var ctx = NewHomeContext(out _, out var games);
        var home = ctx.Render<Home>();
        TypeName(home);
        ChooseBestOf5(home);
        home.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Iniciar partida solo")).Click();

        Assert.Equal("Melhor de 5 • Rodada 1 de 5", home.Find("[data-series-header]").TextContent.Trim());
        var markers = home.FindAll("[data-series-marker]");
        Assert.Equal(6, markers.Count);
        Assert.All(markers, m => Assert.Equal("false", m.GetAttribute("data-filled")));

        home.FindAll("[data-cell]")[4].Click();
        Assert.Equal(Player.X, Assert.Single(games.Values).Board[4]);
    }
}
