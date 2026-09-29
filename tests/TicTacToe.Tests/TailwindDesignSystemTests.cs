using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Layout;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0029: pipeline Tailwind, tokens e fontes Cyber Arena (ADR-0008, G3)

public class TailwindDesignSystemTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static string Web(string relative) => Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web", relative);
    private static string Read(string path)
    {
        Assert.True(File.Exists(path), $"{path} deve existir");
        return File.ReadAllText(path);
    }

    [Fact(DisplayName = "SPEC-0029:UT-01 — Versão do Tailwind pinada e instalação verificada por sha256")]
    [Trait("Category", "SPEC-0029:UT-01")]
    public void TailwindVersion_ShouldBePinned_AndInstallVerifiesChecksum()
    {
        var version = Read(Path.Combine(RootDir, "tools/tailwind/version.txt")).Trim();
        Assert.Matches(@"^\d+\.\d+\.\d+$", version);

        var install = Read(Path.Combine(RootDir, "tools/tailwind/install.sh"));
        Assert.Contains("sha256sums.txt", install);
        Assert.Contains("github.com/tailwindlabs/tailwindcss/releases/download", install);
        Assert.Contains("version.txt", install);
        Assert.Contains("exit 1", install);

        var checksumPos = install.IndexOf("sha256", StringComparison.Ordinal);
        var chmodPos = install.IndexOf("chmod +x", StringComparison.Ordinal);
        Assert.True(checksumPos >= 0 && chmodPos > checksumPos, "chmod +x só pode ocorrer depois da verificação do sha256");
        Assert.DoesNotContain("| sh", install);
        Assert.DoesNotContain("| bash", install);
    }

    private static readonly string[] RequiredTokens =
    [
        "--color-primary: #ff4757", "--color-secondary: #00d2d3", "--color-success: #10b981",
        "--color-gold: #f39c12", "--color-warning: #fbbf24",
        "--color-canvas: #1a1a2e", "--color-surface: #16213e", "--color-surface-high: #0f3460",
        "--color-stroke", "--color-stroke-active", "--color-ink", "--color-ink-muted",
        "--font-display", "--font-numeric",
        "--radius-cell", "--radius-cell-lg", "--radius-card", "--radius-pill",
        "--spacing-gutter", "--spacing-gutter-desktop", "--spacing-margin", "--spacing-margin-desktop",
        "--size-board-mobile", "--size-board-desktop", "--size-frame",
        "--animate-win-pulse",
    ];

    [Fact(DisplayName = "SPEC-0029:UT-02 — CSS de entrada define todos os tokens em @theme")]
    [Trait("Category", "SPEC-0029:UT-02")]
    public void InputCss_ShouldDefineAllTokens_WithoutPreflight()
    {
        var css = Read(Web("Styles/cyber-arena.input.css"));

        Assert.Contains("@theme", css);
        foreach (var token in RequiredTokens)
            Assert.Contains(token, css);

        Assert.Contains("@source", css);
        Assert.Contains("Components", css);
        // Preflight foi habilitado pela SPEC-0034 (fim da coexistência com o MudBlazor); UT-04 da SPEC-0034 o cobre.
        Assert.DoesNotContain("@import \"tailwindcss\"", css);
    }

    [Fact(DisplayName = "SPEC-0029:UT-03 — Fontes locais com font-display swap e sem Google Fonts")]
    [Trait("Category", "SPEC-0029:UT-03")]
    public void Fonts_ShouldBeSelfHosted()
    {
        var input = Read(Web("Styles/cyber-arena.input.css"));
        var generated = Read(Web("wwwroot/css/cyber-arena.css"));

        var urls = Regex.Matches(input, @"url\(['""]?(?<u>[^'"")]+\.woff2)['""]?\)")
            .Select(m => m.Groups["u"].Value).ToList();
        Assert.True(urls.Count >= 2, "Outfit e Space Grotesk devem ter @font-face com woff2");
        Assert.Contains(urls, u => u.Contains("outfit", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(urls, u => u.Contains("space-grotesk", StringComparison.OrdinalIgnoreCase));
        foreach (var url in urls)
            Assert.True(File.Exists(Web("wwwroot" + (url.StartsWith('/') ? url : "/" + url.TrimStart('.', '/')))),
                $"arquivo de fonte {url} deve existir em wwwroot");

        Assert.Contains("font-display:swap", input.Replace(" ", ""));
        foreach (var css in new[] { input, generated })
        {
            Assert.DoesNotContain("fonts.googleapis.com", css);
            Assert.DoesNotContain("fonts.gstatic.com", css);
        }
    }

    [Fact(DisplayName = "SPEC-0029:IT-01 — CSS gerado versionado contém tokens e cabe no orçamento")]
    [Trait("Category", "SPEC-0029:IT-01")]
    public void GeneratedCss_ShouldContainTokens_AndFitBudget()
    {
        var path = Web("wwwroot/css/cyber-arena.css");
        var css = Read(path);

        Assert.Contains("--color-primary:#ff4757", css.Replace(" ", ""));
        Assert.Contains("--font-display", css);
        Assert.Contains("--font-numeric", css);
        Assert.Contains("@font-face", css);
        Assert.True(new FileInfo(path).Length <= 60 * 1024, "CSS gerado deve pesar no máximo 60 KB");
    }

    [Fact(DisplayName = "SPEC-0029:IT-02 — App.razor referencia o CSS novo depois de app.css")]
    [Trait("Category", "SPEC-0029:IT-02")]
    public void AppRazor_ShouldReferenceCyberArenaCssAfterAppCss()
    {
        var app = Read(Web("Components/App.razor"));

        var appCss = app.IndexOf("app.css", StringComparison.Ordinal);
        var cyber = app.IndexOf("css/cyber-arena.css", StringComparison.Ordinal);
        Assert.True(cyber > appCss && appCss >= 0, "cyber-arena.css deve vir depois de app.css");
        // A coexistência com o MudBlazor terminou na SPEC-0034; UT-05 da SPEC-0034 garante a ausência.
    }

    [Fact(DisplayName = "SPEC-0029:IT-03 — CI faz cache do binário por versão e confere drift")]
    [Trait("Category", "SPEC-0029:IT-03")]
    public void Workflow_ShouldCacheBinary_AndCheckDrift()
    {
        var workflow = Read(Path.Combine(RootDir, ".github/workflows/dotnet.yml"));

        Assert.Contains("actions/cache", workflow);
        Assert.Contains("hashFiles('tools/tailwind/version.txt')", workflow);
        Assert.Contains("tools/tailwind/install.sh", workflow);
        Assert.Contains("tools/tailwind/build.sh --check", workflow);
    }

    // ---- SPEC-0034: remoção do MudBlazor e do Bootstrap ----

    private static IEnumerable<string> WebSourceFiles(params string[] extensions) =>
        Directory.EnumerateFiles(Web(""), "*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f)))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"));

    private static DbContextOptions<GameplayDbContext> NewDb() =>
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static BunitContext NewContextWithoutMud(DbContextOptions<GameplayDbContext> options)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ShellState());
        ctx.Services.AddSingleton<IPlayerStorage>(new InMemoryPlayerStorage());
        ctx.Services.AddSingleton<PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return ctx;
    }

    [Fact(DisplayName = "SPEC-0034:CH-01 — Lobby, arena, histórico e ranking renderizam seus elementos-chave (rede de segurança)")]
    [Trait("Category", "SPEC-0034:CH-01")]
    public async Task FourScreens_ShouldRenderKeyElements()
    {
        var options = NewDb();
        await using (var db = new GameplayDbContext(options))
        {
            db.MatchResults.Add(new MatchResult { PlayerXName = "A", PlayerOName = "B", WinnerName = "A" });
            await db.SaveChangesAsync();
        }
        await using var ctx = NewContextWithoutMud(options);

        var lobby = ctx.Render<Lobby>(p => p.Add(l => l.PlayerName, "Thomas"));
        Assert.Contains("Procurar oponente", lobby.Markup);

        using var game = new GameSession(enableBackgroundTimer: false);
        game.SetPlayerName(Player.X, "A");
        game.SetPlayerName(Player.O, "B");
        var arena = ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X));
        Assert.Equal(9, arena.FindAll("[data-cell]").Count);

        var history = ctx.Render<History>();
        // O histórico abre no escopo pessoal (SPEC-0038); a partida semeada é antiga, então só aparece em "Todos".
        history.WaitForAssertion(() => history.FindAll("[role='radiogroup'][aria-label='Escopo'] [role='radio']").Single(r => r.TextContent.Trim() == "Todos").Click());
        history.WaitForAssertion(() => Assert.Single(history.FindAll("tbody tr")));

        var leaderboard = ctx.Render<Leaderboard>();
        leaderboard.WaitForAssertion(() => Assert.Single(leaderboard.FindAll("tbody tr")));
    }

    [Fact(DisplayName = "SPEC-0034:UT-01 — Projetos Web e de testes sem PackageReference a MudBlazor")]
    [Trait("Category", "SPEC-0034:UT-01")]
    public void Csprojs_ShouldNotReferenceMudBlazor()
    {
        Assert.DoesNotContain("MudBlazor", Read(Web("TicTacToe.Web.csproj")));
        Assert.DoesNotContain("MudBlazor", Read(Path.Combine(RootDir, "tests/TicTacToe.Tests/TicTacToe.Tests.csproj")));
    }

    [Fact(DisplayName = "SPEC-0034:UT-02 — Código-fonte da Web sem MudBlazor, mud- ou Bootstrap")]
    [Trait("Category", "SPEC-0034:UT-02")]
    public void WebSources_ShouldNotMentionLegacyLibraries()
    {
        var insensitive = new Regex(@"MudBlazor|mud-|bootstrap", RegexOptions.IgnoreCase);
        var componentPrefix = new Regex(@"\bMud[A-Z]\w*"); // sensível a maiúsculas: não pega palavras como "mudar"
        var offenders = WebSourceFiles(".razor", ".cs", ".css", ".js")
            .Select(f => (File: f, Text: File.ReadAllText(f)))
            .Where(x => insensitive.IsMatch(x.Text) || componentPrefix.IsMatch(x.Text))
            .Select(x => x.File)
            .Select(f => Path.GetRelativePath(RootDir, f))
            .ToList();

        Assert.True(offenders.Count == 0, "Referências legadas em: " + string.Join(", ", offenders));
    }

    [Fact(DisplayName = "SPEC-0034:UT-03 — Nenhum .razor com tag <style>")]
    [Trait("Category", "SPEC-0034:UT-03")]
    public void Razor_ShouldNotContainInlineStyleTags()
    {
        var offenders = WebSourceFiles(".razor").Where(f => File.ReadAllText(f).Contains("<style", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(RootDir, f)).ToList();

        Assert.True(offenders.Count == 0, "<style> em: " + string.Join(", ", offenders));
    }

    [Fact(DisplayName = "SPEC-0034:UT-04 — Preflight habilitado no CSS de entrada e presente no CSS gerado")]
    [Trait("Category", "SPEC-0034:UT-04")]
    public void Preflight_ShouldBeEnabled()
    {
        var input = Read(Web("Styles/cyber-arena.input.css"));
        var generated = Read(Web("wwwroot/css/cyber-arena.css")).Replace(" ", "");

        Assert.Contains("tailwindcss/preflight.css", input);
        Assert.Contains("box-sizing:border-box", generated);
    }

    [Fact(DisplayName = "SPEC-0034:UT-05 — App.razor só com CSS Cyber Arena, sem Mud, Bootstrap ou fontes remotas")]
    [Trait("Category", "SPEC-0034:UT-05")]
    public void AppRazor_ShouldOnlyReferenceCyberArenaAssets()
    {
        var app = Read(Web("Components/App.razor"));

        Assert.Contains("css/cyber-arena.css", app);
        foreach (var forbidden in new[] { "MudBlazor", "bootstrap", "fonts.googleapis.com", "fonts.gstatic.com" })
            Assert.DoesNotContain(forbidden, app, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "SPEC-0034:UT-06 — Bootstrap, Counter e NavMenu removidos")]
    [Trait("Category", "SPEC-0034:UT-06")]
    public void LegacyFiles_ShouldBeRemoved()
    {
        Assert.False(Directory.Exists(Web("wwwroot/lib/bootstrap")));
        Assert.False(File.Exists(Web("Components/Pages/Counter.razor")));
        Assert.False(File.Exists(Web("Components/Layout/NavMenu.razor")));
    }

    [Fact(DisplayName = "SPEC-0034:UT-07 — Página de erro e aviso de erro do Blazor usam os tokens")]
    [Trait("Category", "SPEC-0034:UT-07")]
    public void ErrorUi_ShouldUseTokens()
    {
        var error = Read(Web("Components/Pages/Error.razor"));
        Assert.DoesNotContain("text-danger", error);
        Assert.Contains("text-primary", error);

        var css = Read(Web("Components/Layout/MainLayout.razor.css"));
        Assert.Contains("var(--color-", css);
    }

    [Fact(DisplayName = "SPEC-0034:IT-01 — Layout e telas renderizam sem serviços do MudBlazor")]
    [Trait("Category", "SPEC-0034:IT-01")]
    public async Task Layout_AndScreens_ShouldRenderWithoutMudServices()
    {
        await using var ctx = NewContextWithoutMud(NewDb());

        var layout = ctx.Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<p>ok</p>"))));
        Assert.Equal(2, layout.FindAll("nav").Count);

        ctx.Render<Lobby>();
        ctx.Render<History>();
        ctx.Render<Leaderboard>();
    }

    [Fact(DisplayName = "SPEC-0034:E2E-01 — Jornada completa: shell e conteúdo de cada tela sem Mud")]
    [Trait("Category", "SPEC-0034:E2E-01")]
    public async Task FullJourney_ShouldRenderShellAndContent_WithoutMud()
    {
        var options = NewDb();
        await using var ctx = NewContextWithoutMud(options);

        foreach (var (expected, body) in new (string, Type)[] { ("Procurar oponente", typeof(Lobby)), ("Histórico de Partidas", typeof(History)), ("Classificação Global", typeof(Leaderboard)) })
        {
            var layout = ctx.Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b =>
            {
                b.OpenComponent(0, body);
                b.CloseComponent();
            })));

            Assert.NotEmpty(layout.FindAll("header"));
            Assert.Equal(2, layout.FindAll("nav").Count);
            Assert.DoesNotContain("mud-", layout.Markup);
            Assert.Contains(expected, layout.Markup);
        }
    }
}
