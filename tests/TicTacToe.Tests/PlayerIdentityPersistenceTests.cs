using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Pages;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0037: identidade — persistência e Home

public class PlayerIdentityPersistenceTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string Migrations = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations");

    private static DbContextOptions<GameplayDbContext> NewOptions() =>
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static BunitContext NewHomeContext(DbContextOptions<GameplayDbContext> options, InMemoryPlayerStorage storage, out ConcurrentDictionary<Guid, GameSession> games)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<MatchmakingService>();
        games = new ConcurrentDictionary<Guid, GameSession>();
        ctx.Services.AddSingleton(games);
        ctx.Services.AddSingleton(new ShellState());
        ctx.Services.AddSingleton<IPlayerStorage>(storage);
        ctx.Services.AddScoped<PlayerIdentityService>();
        ctx.Services.AddTransient(_ => new GameplayDbContext(options));
        ctx.Services.AddTransient(sp => new GameResultService(sp.GetRequiredService<GameplayDbContext>(), NullLogger<GameResultService>.Instance));
        return ctx;
    }

    private static InMemoryPlayerStorage StorageWith(Guid id, string nick)
    {
        var storage = new InMemoryPlayerStorage();
        storage.Data["xo.player"] = JsonSerializer.Serialize(new { id, nick });
        return storage;
    }

    [Fact(DisplayName = "SPEC-0037:IT-01 — Gravação com PlayerXId/PlayerOId e migration aditiva com índices")]
    [Trait("Category", "SPEC-0037:IT-01")]
    public async Task SaveResultAsync_ShouldPersistPlayerIds_AndMigrationIsAdditive()
    {
        var options = NewOptions();
        var (x, o) = (Guid.NewGuid(), Guid.NewGuid());
        await using (var db = new GameplayDbContext(options))
        {
            var service = new GameResultService(db, NullLogger<GameResultService>.Instance);

            using var online = new GameSession(enableBackgroundTimer: false);
            online.SetPlayerName(Player.X, "Ana");
            online.SetPlayerName(Player.O, "Bia");
            online.SetPlayerId(Player.X, x);
            online.SetPlayerId(Player.O, o);
            foreach (var (c, p) in new[] { (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X) }) online.MakeMove(c, p);
            await service.SaveResultAsync(online);

            using var solo = new GameSession(enableBackgroundTimer: false) { Mode = GameMode.Solo };
            solo.SetPlayerName(Player.X, "Ana");
            solo.SetPlayerName(Player.O, "Robô");
            solo.SetPlayerId(Player.X, x);
            foreach (var (c, p) in new[] { (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X) }) solo.MakeMove(c, p);
            await service.SaveResultAsync(solo);
        }

        await using var check = new GameplayDbContext(options);
        var rows = await check.MatchResults.ToListAsync();
        var onlineRow = rows.Single(r => r.Mode == GameMode.Online);
        Assert.Equal(x, onlineRow.PlayerXId);
        Assert.Equal(o, onlineRow.PlayerOId);
        var soloRow = rows.Single(r => r.Mode == GameMode.Solo);
        Assert.Equal(x, soloRow.PlayerXId);
        Assert.Null(soloRow.PlayerOId);

        var file = Directory.GetFiles(Migrations, "*_AddPlayerIdentity.cs").Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var full = File.ReadAllText(file);
        var up = full[full.IndexOf("void Up(", StringComparison.Ordinal)..full.IndexOf("void Down(", StringComparison.Ordinal)];
        Assert.Equal(2, Regex.Count(up, @"AddColumn<"));
        Assert.Equal(2, Regex.Count(up, @"CreateIndex\("));
        Assert.Equal(2, Regex.Count(up, @"nullable: true"));
        Assert.DoesNotContain("DropColumn", up);
        Assert.DoesNotContain("AlterColumn", up);
        Assert.DoesNotContain("DropTable", up);
    }

    [Fact(DisplayName = "SPEC-0037:IT-02 — Home carrega o apelido salvo, salva ao jogar e atribui o PlayerId ao jogador X")]
    [Trait("Category", "SPEC-0037:IT-02")]
    public async Task Home_ShouldPrefillNickname_SaveIt_AndAssignPlayerId()
    {
        var id = Guid.NewGuid();
        var storage = StorageWith(id, "Thomas");
        await using var ctx = NewHomeContext(NewOptions(), storage, out var games);

        var home = ctx.Render<Home>();
        home.WaitForAssertion(() => Assert.Equal("Thomas", home.Find("input#playerName").GetAttribute("value")));

        home.Find("input#playerName").Input("Thomas Novo");
        home.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Iniciar partida solo")).Click();

        home.WaitForAssertion(() => Assert.Contains("Thomas Novo", storage.Data["xo.player"]));
        var game = Assert.Single(games.Values);
        Assert.Equal(id, game.GetPlayerId(Player.X));
        Assert.Null(game.GetPlayerId(Player.O));
    }

    [Fact(DisplayName = "SPEC-0037:E2E-01 — Jornada de retorno: boas-vindas, nota de privacidade e partida gravada com o PlayerId")]
    [Trait("Category", "SPEC-0037:E2E-01")]
    public async Task ReturningPlayer_ShouldSeeWelcome_AndSavedGameCarriesPlayerId()
    {
        var id = Guid.NewGuid();
        var options = NewOptions();
        await using var ctx = NewHomeContext(options, StorageWith(id, "Thomas"), out var games);

        var home = ctx.Render<Home>();
        home.WaitForAssertion(() => Assert.Contains("Bem-vindo de volta, Thomas", home.Markup));
        Assert.Contains("salvo neste navegador", home.Markup);

        home.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains("Iniciar partida solo")).Click();
        var game = Assert.Single(games.Values);
        foreach (var (c, p) in new[] { (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X) }) game.MakeMove(c, p);

        MatchResult? row = null;
        for (var i = 0; i < 50 && row is null; i++)
        {
            await using var poll = new GameplayDbContext(options);
            row = await poll.MatchResults.FirstOrDefaultAsync();
            if (row is null) await Task.Delay(100);
        }

        Assert.NotNull(row);
        Assert.Equal(id, row.PlayerXId);
        Assert.Null(row.PlayerOId);
    }
}
