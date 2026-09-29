using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

public class ReactiveEventsAndMigrationsTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact(DisplayName = "SPEC-0025:UT-01 — Home.razor.cs opera puramente por eventos sem temporizador de polling")]
    [Trait("Category", "SPEC-0025:UT-01")]
    public void HomeRazorCs_ShouldNotContainPollingTimer()
    {
        var homeCsPath = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs");
        Assert.True(File.Exists(homeCsPath), "Home.razor.cs deve existir");

        var content = File.ReadAllText(homeCsPath);
        Assert.DoesNotContain("System.Threading.Timer", content);
        Assert.DoesNotContain("_pollTimer", content);
        Assert.DoesNotContain("OnPollTick", content);
        Assert.DoesNotContain("CheckMatchStatus", content);
    }

    [Fact(DisplayName = "SPEC-0025:UT-02 — TicTacToe.Modules.Gameplay contém migrações EF Core e ModelSnapshot")]
    [Trait("Category", "SPEC-0025:UT-02")]
    public void GameplayModule_ShouldContainMigrationsAndModelSnapshot()
    {
        var gameplayAssembly = typeof(GameplayDbContext).Assembly;
        var migrationTypes = gameplayAssembly.GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        var snapshotTypes = gameplayAssembly.GetTypes()
            .Where(t => typeof(ModelSnapshot).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(migrationTypes);
        Assert.NotEmpty(snapshotTypes);
    }

    [Fact(DisplayName = "SPEC-0025:IT-01 — Program.cs executa MigrateAsync em vez de EnsureCreatedAsync")]
    [Trait("Category", "SPEC-0025:IT-01")]
    public void ProgramStartup_ShouldUseMigrateAsync()
    {
        var programPath = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Program.cs");
        Assert.True(File.Exists(programPath), "Program.cs deve existir");

        var content = File.ReadAllText(programPath);
        Assert.Contains("MigrateAsync()", content);
        Assert.DoesNotContain("EnsureCreatedAsync()", content);
    }

    [Fact(DisplayName = "SPEC-0025:E2E-01 — Sessão de jogo e eventos reativos operam sem polling")]
    [Trait("Category", "SPEC-0025:E2E-01")]
    public void GameSession_ShouldNotifyReactivityDirectly()
    {
        var game = new GameSession();
        int stateChangedCount = 0;
        game.OnStateChanged += () => stateChangedCount++;

        game.MakeMove(0, Player.X);
        Assert.Equal(1, stateChangedCount);

        game.MakeMove(1, Player.O);
        Assert.Equal(2, stateChangedCount);
    }
}
