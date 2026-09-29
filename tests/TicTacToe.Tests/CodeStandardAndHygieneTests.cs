using System;
using System.IO;
using System.Linq;
using Xunit;

namespace TicTacToe.Tests;

public class CodeStandardAndHygieneTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact(DisplayName = "SPEC-0022:UT-01 — Directory.Build.props centraliza TreatWarningsAsErrors e Nullable")]
    [Trait("Category", "SPEC-0022:UT-01")]
    public void DirectoryBuildProps_ShouldEnforceStrictQuality()
    {
        var propsPath = Path.Combine(RootDir, "Directory.Build.props");
        Assert.True(File.Exists(propsPath), "Directory.Build.props deve existir na raiz do repositório");

        var content = File.ReadAllText(propsPath);
        Assert.Contains("<Nullable>enable</Nullable>", content);
        Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", content);
    }

    [Fact(DisplayName = "SPEC-0022:UT-02 — Nenhum script temporário fix_*.py permanece na raiz")]
    [Trait("Category", "SPEC-0022:UT-02")]
    public void RootDirectory_ShouldNotContainTemporaryFixScripts()
    {
        var fixScripts = Directory.GetFiles(RootDir, "fix_*.py");
        Assert.Empty(fixScripts);
    }

    [Fact(DisplayName = "SPEC-0022:IT-01 — EditorConfig existe e define regras de formatação")]
    [Trait("Category", "SPEC-0022:IT-01")]
    public void EditorConfig_ShouldExistAndDefineFormattingRules()
    {
        var editorConfigPath = Path.Combine(RootDir, ".editorconfig");
        Assert.True(File.Exists(editorConfigPath), ".editorconfig deve existir na raiz do repositório");

        var content = File.ReadAllText(editorConfigPath);
        Assert.Contains("root = true", content);
        Assert.Contains("indent_size = 4", content);
    }
}
