using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

    [Fact(DisplayName = "SPEC-0029:UT-02 — CSS de entrada define todos os tokens em @theme, sem preflight")]
    [Trait("Category", "SPEC-0029:UT-02")]
    public void InputCss_ShouldDefineAllTokens_WithoutPreflight()
    {
        var css = Read(Web("Styles/cyber-arena.input.css"));

        Assert.Contains("@theme", css);
        foreach (var token in RequiredTokens)
            Assert.Contains(token, css);

        Assert.Contains("@source", css);
        Assert.Contains("Components", css);
        Assert.DoesNotContain("preflight", css, StringComparison.OrdinalIgnoreCase);
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

    [Fact(DisplayName = "SPEC-0029:IT-02 — App.razor referencia o CSS novo e mantém MudBlazor")]
    [Trait("Category", "SPEC-0029:IT-02")]
    public void AppRazor_ShouldReferenceCyberArenaCss_AndKeepMudBlazor()
    {
        var app = Read(Web("Components/App.razor"));

        var appCss = app.IndexOf("app.css", StringComparison.Ordinal);
        var cyber = app.IndexOf("css/cyber-arena.css", StringComparison.Ordinal);
        Assert.True(cyber > appCss && appCss >= 0, "cyber-arena.css deve vir depois de app.css");
        Assert.Contains("MudBlazor.min.css", app);
        Assert.Contains("MudBlazor.min.js", app);
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
}
