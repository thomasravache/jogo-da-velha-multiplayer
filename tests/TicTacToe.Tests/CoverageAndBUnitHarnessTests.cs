using System;
using System.IO;
using Xunit;

namespace TicTacToe.Tests;

public class CoverageAndBUnitHarnessTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact(DisplayName = "SPEC-0023:UT-01 — sdd-config.yml configura comando de cobertura")]
    [Trait("Category", "SPEC-0023:UT-01")]
    public void SddConfig_ShouldConfigureCoverageCommand()
    {
        var configPath = Path.Combine(RootDir, "docs/specs/sdd-config.yml");
        Assert.True(File.Exists(configPath), "sdd-config.yml deve existir");

        var content = File.ReadAllText(configPath);
        Assert.Contains("coverage: \"dotnet test", content);
        Assert.Contains("Code Coverage", content);
    }

    [Fact(DisplayName = "SPEC-0023:UT-02 — TicTacToe.Tests.csproj referencia pacotes bunit e coverlet.collector")]
    [Trait("Category", "SPEC-0023:UT-02")]
    public void TestsProject_ShouldReferenceBunitAndCoverlet()
    {
        var csprojPath = Path.Combine(RootDir, "tests/TicTacToe.Tests/TicTacToe.Tests.csproj");
        Assert.True(File.Exists(csprojPath), "TicTacToe.Tests.csproj deve existir");

        var content = File.ReadAllText(csprojPath);
        Assert.Contains("bunit", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("coverlet.collector", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "SPEC-0023:IT-01 — Harness bUnit renderiza fragmentos Blazor em memória")]
    [Trait("Category", "SPEC-0023:IT-01")]
    public void BUnitHarness_ShouldRenderComponentInMemory()
    {
        // Valida que o assembly bunit está carregado e funcional
        var bunitAssembly = typeof(Bunit.BunitContext).Assembly;
        Assert.NotNull(bunitAssembly);

        using var ctx = new Bunit.BunitContext();
        var cut = ctx.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "game-cell");
            builder.AddContent(2, "✕");
            builder.CloseElement();
        });

        Assert.Contains("game-cell", cut.Markup);
        Assert.Contains("✕", cut.Markup);
    }
}
