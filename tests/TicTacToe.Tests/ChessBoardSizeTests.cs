using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Chess;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.Chess;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0062: tabuleiro de xadrez maior no desktop (tokens de tamanho e classes que os usam)

public sealed class ChessBoardSizeTests : IDisposable
{
    private readonly List<BunitContext> _contexts = [];

    public void Dispose()
    {
        foreach (var ctx in _contexts)
        {
            ctx.Dispose();
        }
    }

    private static string ReadInputCss()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TicTacToe.sln")) && !Directory.Exists(Path.Combine(dir.FullName, ".git")) && !File.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir.FullName, "src", "TicTacToe", "TicTacToe.Web", "Styles", "cyber-arena.input.css"));
    }

    private static string Token(string css, string name)
    {
        var match = Regex.Match(css, @"--" + Regex.Escape(name) + @"\s*:\s*([^;]+);", RegexOptions.None, TimeSpan.FromSeconds(5));
        Assert.True(match.Success, $"Token --{name} não encontrado.");
        return match.Groups[1].Value.Trim();
    }

    private IRenderedComponent<ChessHome> OpenHome()
    {
        var storage = new InMemoryPlayerStorage();
        storage.Data["xo.player"] = JsonSerializer.Serialize(new { id = Guid.NewGuid(), nick = "Ana" });
        var options = new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new MatchmakingService());
        ctx.Services.AddSingleton(new ChessMatchRegistry());
        ctx.Services.AddSingleton<TimeProvider>(new ManualTime());
        ctx.Services.AddSingleton<Func<bool>>(() => true);
        ctx.Services.AddSingleton(new ShellState());
        ctx.Services.AddSingleton<IPlayerStorage>(storage);
        ctx.Services.AddScoped<PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        ctx.Services.AddScoped(sp => new ChessResultRecorder(sp.GetRequiredService<GameResultService>(), NullLogger<ChessResultRecorder>.Instance));
        _contexts.Add(ctx);
        var cut = ctx.Render<ChessHome>();
        cut.WaitForAssertion(() => Assert.Equal("Ana", cut.Find("input#playerName").GetAttribute("value")));
        return cut;
    }

    private static void Choose(IRenderedComponent<ChessHome> cut, string text) =>
        cut.FindAll("[role='radio']").First(r => r.TextContent.Contains(text, StringComparison.Ordinal)).Click();

    [Fact(DisplayName = "SPEC-0062:UT-01 — ChessBoard limita a largura por --size-board-chess")]
    [Trait("Category", "SPEC-0062:UT-01")]
    public void Board_ShouldUseChessBoardSizeToken()
    {
        var ctx = new BunitContext();
        _contexts.Add(ctx);
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<ChessBoard>(p => p.Add(c => c.Position, Position.Start));

        var classes = cut.Find("[data-board]").GetAttribute("class") ?? string.Empty;
        Assert.Contains("max-w-[var(--size-board-chess)]", classes, StringComparison.Ordinal);
        Assert.DoesNotContain("--size-board-desktop", classes, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0062:UT-02 — Partida usa --size-frame-chess; lobby continua em --size-frame")]
    [Trait("Category", "SPEC-0062:UT-02")]
    public void Home_ShouldUseWideFrameOnlyInMatchView()
    {
        var cut = OpenHome();

        Assert.NotEmpty(cut.FindAll("div[class*='max-w-[var(--size-frame)]']"));
        Assert.Empty(cut.FindAll("div[class*='--size-frame-chess']"));

        Choose(cut, "Fácil");
        Choose(cut, "Brancas");
        cut.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Iniciar partida solo", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<ChessArena>()));

        Assert.NotEmpty(cut.FindAll("div[class*='max-w-[var(--size-frame-chess)]']"));
        Assert.Empty(cut.FindAll("div[class*='max-w-[var(--size-frame)]']"));
    }

    [Fact(DisplayName = "SPEC-0062:UT-03 — Tokens de tamanho do xadrez no CSS de entrada")]
    [Trait("Category", "SPEC-0062:UT-03")]
    public void Css_ShouldDefineChessSizeTokens()
    {
        var css = ReadInputCss();

        Assert.Equal("80rem", Token(css, "size-frame-chess"));
        var board = Token(css, "size-board-chess");
        Assert.Contains("42rem", board, StringComparison.Ordinal);
        Assert.Contains("100dvh", board, StringComparison.Ordinal);
        Assert.Equal("480px", Token(css, "size-board-desktop"));
        Assert.Equal("960px", Token(css, "size-frame"));
    }
}
