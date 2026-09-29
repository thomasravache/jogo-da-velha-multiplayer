using System.Xml.Linq;

namespace TicTacToe.Tests;

public class ChessModuleBoundaryTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    private static List<string> ProjectReferences(string module)
    {
        var path = Path.Combine(RootDir, "src", "TicTacToe", module, module + ".csproj");
        Assert.True(File.Exists(path), $"csproj não encontrado: {path}");

        return [.. XDocument.Load(path)
            .Descendants("ProjectReference")
            .Select(e => (string?)e.Attribute("Include") ?? string.Empty)];
    }

    [Fact(DisplayName = "SPEC-0049:IT-02 — Chess não referencia Gameplay, Matchmaking nem Web")]
    [Trait("Category", "SPEC-0049:IT-02")]
    [Trait("Category", "Architecture")]
    public void Chess_ShouldNotReferenceOtherModules()
    {
        var references = ProjectReferences("TicTacToe.Modules.Chess");

        Assert.DoesNotContain(references, r => r.Contains("Gameplay", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => r.Contains("Matchmaking", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => r.Contains("TicTacToe.Web", StringComparison.Ordinal));
    }

    [Theory(DisplayName = "SPEC-0049:IT-02 — Gameplay e Matchmaking não referenciam Chess")]
    [Trait("Category", "SPEC-0049:IT-02")]
    [Trait("Category", "Architecture")]
    [InlineData("TicTacToe.Modules.Gameplay")]
    [InlineData("TicTacToe.Modules.Matchmaking")]
    public void OtherModules_ShouldNotReferenceChess(string module) =>
        Assert.DoesNotContain(ProjectReferences(module), r => r.Contains("Chess", StringComparison.Ordinal));
}
