using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Layout;
using TicTacToe.Web.Components.Ui;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0043: shell e primitivos de UI Cyber Arena — shell (layout, navegação, modo imersivo)

public class ShellLayoutTests
{
    private static readonly string[] NavHrefs = ["/", "/history", "/leaderboard"];
    private static readonly string[] NavLabels = ["Jogar", "Histórico", "Ranking"];
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    private static BunitContext NewContext(out ShellState shell)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        shell = new ShellState();
        ctx.Services.AddSingleton(shell);
        return ctx;
    }

    private static IRenderedComponent<MainLayout> RenderLayout(BunitContext ctx) =>
        ctx.Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"corpo\">ok</p>"))));

    [Fact(DisplayName = "SPEC-0043:UT-01 — Header com marca e navegação principal em pílulas a partir de lg")]
    [Trait("Category", "SPEC-0043:UT-01")]
    public async Task Header_ShouldRenderBrandAndDesktopNav()
    {
        await using var ctx = NewContext(out _);
        var cut = RenderLayout(ctx);

        var brand = cut.Find("header a[href='/']");
        Assert.NotNull(brand.QuerySelector("img[alt]"));

        var nav = cut.Find("header nav[aria-label='Principal']");
        var links = nav.QuerySelectorAll("a").ToList();
        Assert.Equal(NavHrefs, links.Select(a => a.GetAttribute("href")).ToArray());
        Assert.Equal(NavLabels, links.Select(a => a.TextContent.Trim()).ToArray());
        var cls = nav.GetAttribute("class") ?? "";
        Assert.Contains("hidden", cls);
        Assert.Contains("lg:flex", cls);
    }

    [Fact(DisplayName = "SPEC-0043:UT-02 — Barra de navegação inferior fixa e escondida em lg")]
    [Trait("Category", "SPEC-0043:UT-02")]
    public async Task BottomNav_ShouldRenderFixedAndHiddenOnLg()
    {
        await using var ctx = NewContext(out _);
        var cut = RenderLayout(ctx);

        var navs = cut.FindAll("nav");
        Assert.Equal(2, navs.Count);
        var bottom = navs.Single(n => (n.GetAttribute("class") ?? "").Contains("fixed"));
        var cls = bottom.GetAttribute("class") ?? "";
        Assert.Contains("bottom-0", cls);
        Assert.Contains("lg:hidden", cls);
        var links = bottom.QuerySelectorAll("a").ToList();
        Assert.Equal(NavHrefs, links.Select(a => a.GetAttribute("href")).ToArray());
        Assert.All(links, a => Assert.NotNull(a.QuerySelector("svg")));
    }

    [Fact(DisplayName = "SPEC-0043:UT-03 — Só o link da rota atual tem aria-current e '/' é correspondência exata")]
    [Trait("Category", "SPEC-0043:UT-03")]
    public async Task ActiveLink_ShouldHaveAriaCurrent_OnlyForCurrentRoute()
    {
        await using var ctx = NewContext(out _);
        var nav = ctx.Services.GetRequiredService<NavigationManager>();

        nav.NavigateTo("/history");
        var cut = RenderLayout(ctx);
        var current = cut.FindAll("header nav a[aria-current='page']");
        Assert.Single(current);
        Assert.Equal("/history", current[0].GetAttribute("href"));

        nav.NavigateTo("/");
        cut.WaitForAssertion(() =>
        {
            var now = cut.FindAll("header nav a[aria-current='page']");
            Assert.Single(now);
            Assert.Equal("/", now[0].GetAttribute("href"));
        });
    }

    [Fact(DisplayName = "SPEC-0043:UT-04 — Modo imersivo esconde a barra inferior e mostra o título no header mobile")]
    [Trait("Category", "SPEC-0043:UT-04")]
    public async Task ImmersiveMode_ShouldHideBottomNav_AndShowTitle()
    {
        await using var ctx = NewContext(out var shell);
        var cut = RenderLayout(ctx);
        Assert.Equal(2, cut.FindAll("nav").Count);

        await cut.InvokeAsync(() => shell.Set(true, "Partida ativa"));
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("nav"));
            Assert.Contains("Partida ativa", cut.Find("header").TextContent);
        });

        await cut.InvokeAsync(() => shell.Reset());
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindAll("nav").Count);
            Assert.DoesNotContain("Partida ativa", cut.Find("header").TextContent);
        });
    }

    [Fact(DisplayName = "SPEC-0043:IT-01 — Lobby renderiza dentro do novo shell")]
    [Trait("Category", "SPEC-0043:IT-01")]
    public async Task LegacyMudLobby_ShouldRenderInsideNewShell()
    {
        await using var ctx = NewContext(out _);
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b =>
        {
            b.OpenComponent<Lobby>(0);
            b.CloseComponent();
        })));

        Assert.Contains("Procurar oponente", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0043:IT-02 — ShellState registrado como Scoped e Set/Reset restauram o shell")]
    [Trait("Category", "SPEC-0043:IT-02")]
    public void ShellState_ShouldBeRegisteredScoped_AndResetRestores()
    {
        var program = File.ReadAllText(Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Program.cs"));
        Assert.Contains("AddScoped<ShellState>", program);

        var shell = new ShellState();
        var changes = 0;
        shell.Changed += () => changes++;
        shell.Set(true, "Partida ativa");
        Assert.True(shell.Immersive);
        Assert.Equal("Partida ativa", shell.Title);
        shell.Reset();
        Assert.False(shell.Immersive);
        Assert.Null(shell.Title);
        Assert.Equal(2, changes);
    }

    [Fact(DisplayName = "SPEC-0043:E2E-01 — Jornada do shell: header, navegação, barra inferior e tokens; sem MudAppBar")]
    [Trait("Category", "SPEC-0043:E2E-01")]
    public async Task Shell_ShouldRenderCyberArenaShell_WithoutLegacyAppBar()
    {
        await using var ctx = NewContext(out _);
        var cut = RenderLayout(ctx);

        Assert.Contains("id=\"corpo\"", cut.Markup);
        Assert.NotEmpty(cut.FindAll("header"));
        Assert.Equal(2, cut.FindAll("nav").Count);
        Assert.Contains("bg-canvas", cut.Markup);
        Assert.Empty(cut.FindAll(".mud-appbar"));
        Assert.Empty(cut.FindAll(".mud-layout"));
    }

    [Fact(DisplayName = "SPEC-0043:UT-11 — Pares de cor de texto atendem contraste AA (4.5:1)")]
    [Trait("Category", "SPEC-0043:UT-11")]
    public void TokenPairs_ShouldMeetWcagAa()
    {
        var css = File.ReadAllText(Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Styles/cyber-arena.input.css"));
        string Hex(string token)
        {
            var m = Regex.Match(css, $@"--color-{token}:\s*(#[0-9a-fA-F]{{6}})");
            Assert.True(m.Success, $"token --color-{token} deve ser hexadecimal de 6 dígitos");
            return m.Groups[1].Value;
        }

        double Lum(string hex)
        {
            double Ch(int i)
            {
                var c = Convert.ToInt32(hex.Substring(1 + i * 2, 2), 16) / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Ch(0) + 0.7152 * Ch(1) + 0.0722 * Ch(2);
        }

        double Ratio(string fg, string bg)
        {
            var a = Lum(Hex(fg)); var b = Lum(Hex(bg));
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        var pairs = new[]
        {
            ("ink", "canvas"), ("ink", "surface"), ("ink-muted", "surface"), ("primary", "canvas"),
            ("canvas", "primary"), ("secondary", "canvas"), ("success", "canvas"), ("gold", "canvas"), ("warning", "canvas"),
        };
        foreach (var (fg, bg) in pairs)
            Assert.True(Ratio(fg, bg) >= 4.5, $"{fg} sobre {bg} tem contraste {Ratio(fg, bg):F2}");
    }

    [Fact(DisplayName = "SPEC-0043:UT-03b — Rota parecida não ativa o link (/historyfoo)")]
    [Trait("Category", "SPEC-0043:UT-03")]
    public async Task SimilarRoute_ShouldNotActivateLink()
    {
        await using var ctx = NewContext(out _);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/historyfoo");
        var cut = RenderLayout(ctx);

        Assert.Empty(cut.FindAll("header nav a[aria-current='page']"));
    }

    [Fact(DisplayName = "SPEC-0043:IT-01b — main em bloco (sem flex central) e links com cor importante para coexistir com CSS legado")]
    [Trait("Category", "SPEC-0043:IT-01")]
    public async Task Layout_ShouldKeepMainInBlockFlow_AndProtectLinkColors()
    {
        await using var ctx = NewContext(out _);
        var cut = RenderLayout(ctx);

        var main = cut.Find("main").GetAttribute("class") ?? "";
        Assert.DoesNotContain("items-center", main);
        Assert.DoesNotContain("justify-center", main);
        Assert.Contains("bg-transparent!", main);

        var linkClass = cut.Find("header nav a").GetAttribute("class") ?? "";
        Assert.Contains("no-underline!", linkClass);
        Assert.Matches(@"text-[a-z-]+!", linkClass);
    }
}
